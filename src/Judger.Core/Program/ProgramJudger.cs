﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Judger.Core.Program.Internal;
using Judger.Core.Program.Internal.Entity;
using Judger.Managers;
using Judger.Models;
using Judger.Models.Exception;
using Judger.Models.Program;

namespace Judger.Core.Program
{
    public class ProgramJudger : BaseJudger
    {
        public ProgramJudger(JudgeTask task) : base(task)
        {
            JudgeTask.ProcessorAffinity = ProcessorAffinityManager.GetUseage();
            LangConfig = JudgeTask.LangConfig as ProgramLangConfig;
        }

        private ProgramLangConfig LangConfig { get; set; }

        public override JudgeResult Judge()
        {
            //判题结果
            JudgeResult result = new JudgeResult
            {
                SubmitId = JudgeTask.SubmitId,
                ProblemId = JudgeTask.ProblemId,
                Author = JudgeTask.Author,
                JudgeDetail = "",
                MemoryCost = 0,
                TimeCost = 0,
                PassRate = 0,
                ResultCode = JudgeResultCode.Accepted
            };

            //正则恶意代码检查
            if (!CodeChecker.Singleton.CheckCode(JudgeTask.SourceCode, JudgeTask.Language, out string unsafeCode,
                out int line))
            {
                result.ResultCode = JudgeResultCode.CompileError;
                result.JudgeDetail = "Include unsafe code, please remove them!";
                result.JudgeDetail += "\r\n";
                result.JudgeDetail += "line " + line + ": " + unsafeCode;
                return result;
            }

            //写出源代码
            string sourceFileName = Path.Combine(JudgeTask.TempJudgeDirectory, LangConfig.SourceCodeFileName);
            File.WriteAllText(sourceFileName, JudgeTask.SourceCode);

            //编译代码
            if (LangConfig.NeedCompile)
            {
                Compiler compiler = new Compiler(JudgeTask);
                string compileRes = compiler.Compile();

                //检查是否有编译错误(compileRes不为空则代表有错误)
                if (!string.IsNullOrEmpty(compileRes))
                {
                    //去除路径信息
                    result.JudgeDetail = compileRes.Replace(JudgeTask.TempJudgeDirectory, "");
                    result.ResultCode = JudgeResultCode.CompileError;
                    return result;
                }
            }

            //创建单例Judger
            SingleCaseJudger judger = new SingleCaseJudger(JudgeTask);

            //获取所有测试点文件名
            Tuple<string, string>[] dataFiles = TestDataManager.GetTestDataFilesName(JudgeTask.ProblemId);
            if (dataFiles.Length == 0) //无测试数据
            {
                result.ResultCode = JudgeResultCode.JudgeFailed;
                result.JudgeDetail = "No test data.";
                return result;
            }

            result.MemoryCost = ConfigManager.Config.MinimumMemoryCost;
            int acceptedCasesCount = 0; //通过的测试点数
            for (int i = 0; i < dataFiles.Length; i++)
            {
                try
                {
                    //读入测试数据
                    TestDataManager.GetTestData(
                        JudgeTask.ProblemId, dataFiles[i].Item1, dataFiles[i].Item2,
                        out string input, out string output);

                    SingleJudgeResult singleRes = judger.Judge(input, output); //测试此测试点

                    //计算有时间补偿的总时间
                    result.TimeCost = Math.Max(result.TimeCost,
                        (int) (singleRes.TimeCost * LangConfig.TimeCompensation));
                    result.MemoryCost = Math.Max(result.MemoryCost, singleRes.MemoryCost);

                    if (singleRes.ResultCode == JudgeResultCode.Accepted)
                    {
                        acceptedCasesCount++;
                    }
                    else
                    {
                        result.ResultCode = singleRes.ResultCode;
                        result.JudgeDetail = singleRes.JudgeDetail;

                        if (!JudgeTask.JudgeAllCases)
                            break;
                    }
                }
                catch (Exception e)
                {
                    result.ResultCode = JudgeResultCode.JudgeFailed;
                    result.JudgeDetail = e.ToString();
                    break;
                }
            }

            //去除目录信息
            result.JudgeDetail = result.JudgeDetail.Replace(JudgeTask.TempJudgeDirectory, "");

            //通过率
            result.PassRate = (double) acceptedCasesCount / dataFiles.Length;

            return result;
        }

        public override void Dispose()
        {
            // 释放占用的独立处理器核心
            ProcessorAffinityManager.ReleaseUseage(JudgeTask.ProcessorAffinity);
            DeleteTempDirectory();
        }

        /// <summary>
        /// 删除临时目录
        /// </summary>
        private void DeleteTempDirectory()
        {
            //判题结束时文件可能仍然被占用，尝试删除
            new Task(() =>
            {
                int tryCount = 0;
                while (true)
                {
                    try
                    {
                        Directory.Delete(JudgeTask.TempJudgeDirectory, true);
                        break;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        break;
                    }
                    catch
                    {
                        if (tryCount++ > 20)
                            throw new JudgeException("Cannot delete temp directory");

                        Thread.Sleep(500);
                    }
                }
            }, TaskCreationOptions.LongRunning).Start();
        }
    }
}