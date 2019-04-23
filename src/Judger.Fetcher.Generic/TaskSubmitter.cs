﻿using Newtonsoft.Json.Linq;
using Judger.Entity;
using Judger.Fetcher.Generic.Entity;

namespace Judger.Fetcher.Generic
{
    /// <summary>
    /// JudgeResult提交器
    /// </summary>
    public class TaskSubmitter : BaseTaskSubmitter
    {
        /// <summary>
        /// JudgeResult提交器
        /// </summary>
        public TaskSubmitter()
        {
            HttpClient.DefaultContentType = "application/json";
        }

        /// <summary>
        /// 提交评测任务
        /// </summary>
        /// <param name="result">评测任务</param>
        /// <returns>提交是否成功</returns>
        public override bool Submit(JudgeResult result)
        {
            HttpClient.UploadString(Config.ResultSubmitUrl, CreateRequestBody(result), 3);
            return true;
        }

        /// <summary>
        /// 创建提交测试结果的请求
        /// </summary>
        /// <returns>提交测试结果的请求</returns>
        private string CreateRequestBody(JudgeResult result)
        {
            InnerJudgeResult judgeResult = new InnerJudgeResult
            {
                SubmitId = result.SubmitId,
                ProblemId = result.ProblemId,
                Author = result.Author,
                JudgeDetail = result.JudgeDetail,
                MemoryCost =  result.MemoryCost,
                PassRate = result.PassRate,
                ResultCode = result.ResultCode,
                TimeCost = result.TimeCost
            };
            
            JObject requestBody = JObject.FromObject(judgeResult);
            Token.AddTokenToJObject(requestBody);

            return requestBody.ToString();
        }
    }
}
