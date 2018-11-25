﻿using System;
using System.Collections.Generic;
using System.Text;
using Judger.Models;

namespace Judger.Managers
{
    /// <summary>
    /// 处理器亲和性管理器, 用于向判题任务分配独立处理器核心
    /// </summary>
    public static class ProcessorAffinityManager
    {
        /// <summary>
        /// 默认处理器亲和性
        /// </summary>
        public static IntPtr DefaultAffinity
        {
            get
            {
                return new IntPtr(_defaultAffinity);
            }
        }

        private static readonly object _lock = new object();

        //CPU核心数
        private static int _processorCount;

        //默认亲和性
        private static int _defaultAffinity = 0;

        //被使用的核心(二进制表示)
        private static int _usingProcessor = 0;

        static ProcessorAffinityManager()
        {
            _processorCount = Environment.ProcessorCount;
            for (int i = 0; i < _processorCount; i++)
            {
                _defaultAffinity += (1 << i);
            }
        }

        /// <summary>
        /// 申请处理器使用权
        /// </summary>
        public static IntPtr GetUseage()
        {
            lock (_lock)
            {
                int affinity = _defaultAffinity;
                for (int i = 0; i < _processorCount; i++)//遍历所有处理器核心
                {
                    if ((_usingProcessor & (1 << i)) == 0)//判断此处理器核心是否被占用
                    {
                        affinity = (1 << i);
                        _usingProcessor |= affinity;
                        break;
                    }
                }
                return new IntPtr(affinity);
            }
        }

        /// <summary>
        /// 释放useage对应处理器
        /// </summary>
        public static void ReleaseUseage(IntPtr affinity)
        {
            int affinityInt = affinity.ToInt32();
            if (affinityInt < _defaultAffinity)
            {
                _usingProcessor ^= affinityInt;
            }
        }
    }
}
