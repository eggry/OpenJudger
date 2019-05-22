﻿using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Timers;

namespace Judger.Utils
{
    /// <summary>
    /// 运行时监控器, 可监控程序运行时间及峰值内存消耗, 可做出限制
    /// </summary>
    public class RuntimeMonitor : IDisposable
    {
        // 指示当前平台是否为Linux
        private bool _platformIsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        // 指示当前平台是否为Windows
        private bool _platformIsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        // 指示子进程是否运行在虚拟机中(如Java, Python)
        private bool _runningInVm;

        private Timer _timer = new Timer();

        /// <summary>
        /// 运行时监控器
        /// </summary>
        /// <param name="process">需要监控的Process</param>
        /// <param name="interval">监控周期</param>
        /// <param name="runningInVm">是否运行在虚拟机中</param>
        public RuntimeMonitor(Process process, int interval = 20, bool runningInVm = false)
        {
            Process = process;
            _timer.Interval = interval;
            _timer.Elapsed += OnMonitor;
            _runningInVm = runningInVm;
        }

        /// <summary>
        /// (CPU)时间消耗
        /// </summary>
        public int TimeCost { get; private set; }

        /// <summary>
        /// 总时间消耗
        /// </summary>
        public int TotalTimeCost { get; private set; }

        /// <summary>
        /// 内存消耗
        /// </summary>
        public int MemoryCost { get; private set; }

        /// <summary>
        /// (CPU)时间限制
        /// </summary>
        public int TimeLimit { get; set; }

        /// <summary>
        /// 最大时间限制
        /// </summary>
        public int TotalTimeLimit { get; set; }

        /// <summary>
        /// 内存限制
        /// </summary>
        public int MemoryLimit { get; set; }

        /// <summary>
        /// 是否因超出限制而Kill掉进程
        /// </summary>
        public bool LimitExceed { get; private set; }

        /// <summary>
        /// 当前监控的Process
        /// </summary>
        public Process Process { get; }

        public void Dispose()
        {
            Stop();
            _timer.Dispose();
        }

        /// <summary>
        /// 开始运行时监控
        /// </summary>
        public void Start()
        {
            _timer.Start();
        }

        /// <summary>
        /// 停止运行时监控
        /// </summary>
        public void Stop()
        {
            try
            {
                CheckTimeCost();
                CheckMemoryCost();
            }
            catch
            { }

            _timer.Stop();
        }

        private void OnMonitor(object sender, ElapsedEventArgs e)
        {
            try //防止无效操作异常
            {
                if (Process.HasExited)
                {
                    Stop();
                    return;
                }

                if (!CheckTimeCost() || !CheckMemoryCost())
                    Process.Kill();
            }
            catch
            { }
        }

        /// <summary>
        /// 检查时间消耗
        /// </summary>
        /// <returns>是否通过检查</returns>
        private bool CheckTimeCost()
        {
            // 使用的CPU时间
            TimeCost = (int) Process.TotalProcessorTime.TotalMilliseconds;
            // 使用的总时间
            TotalTimeCost = (int) (DateTime.Now - Process.StartTime).TotalMilliseconds;

            if ((TimeCost > TimeLimit && TimeLimit > 0) ||
                (TotalTimeCost > TotalTimeLimit && TotalTimeLimit > 0))
            {
                TimeCost = TimeLimit;
                TotalTimeCost = TotalTimeLimit;
                LimitExceed = true;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查内存消耗
        /// </summary>
        /// <returns>是否通过检查</returns>
        private bool CheckMemoryCost()
        {
            int nowMemoryCost = PeakMemory();

            if (nowMemoryCost > MemoryCost)
            {
                MemoryCost = nowMemoryCost;
                if (MemoryCost > MemoryLimit && MemoryLimit > 0)
                {
                    MemoryCost = MemoryLimit;
                    LimitExceed = true;
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 获取进程内存使用的峰值
        /// </summary>
        /// <returns>进程内存使用的峰值</returns>
        private int PeakMemory()
        {
            // 分平台实现
            if (_platformIsWindows)
                return PeakMemoryOnWindows();

            if (_platformIsLinux)
                return PeakMemoryOnLinux();

            return PeakMemoryOnUnknown();
        }

        /// <summary>
        /// Windows下获取进程内存使用的峰值
        /// </summary>
        /// <returns>进程内存使用的峰值</returns>
        private int PeakMemoryOnWindows()
        {
            if (_runningInVm)
                return (int) (Process.PeakWorkingSet64 / 1024);

            return (int) (Process.PeakPagedMemorySize64 / 1024);
        }

        /// <summary>
        /// Linux下获取进程内存使用的峰值
        /// </summary>
        /// <returns>进程内存使用的峰值</returns>
        /// 
        /// 方法从/proc/ProcessId/status中解析峰值内存使用
        /// VmPeak为内存使用峰值, 适合大多数情况
        /// VmHWM为物理内存使用峰值, 适合运行在虚拟机中的语言
        private int PeakMemoryOnLinux()
        {
            string queryKey = _runningInVm ? "VmHWM" : "VmPeak";

            string[] lines = File.ReadAllLines(string.Format("/proc/{0}/status", Process.Id));
            foreach (string line in lines)
            {
                if (!line.StartsWith(queryKey))
                    continue;

                string[] splits = line.Split(' ');
                string vmPeakStr = splits[splits.Length - 2];

                return int.Parse(vmPeakStr);
            }

            return 0;
        }

        /// <summary>
        /// 未知平台下获取进程的内存使用量
        /// </summary>
        /// <returns>进程的内存使用量</returns>
        /// 
        /// .Net Core确保所有平台下都实现了WorkingSet64属性, 但此值是不可靠的
        private int PeakMemoryOnUnknown()
        {
            return (int) (Process.WorkingSet64 / 1024);
        }
    }
}