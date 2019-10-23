﻿namespace Judger.Models.Exception
{
    /// <summary>
    /// 判题异常
    /// </summary>
    public class JudgeException : BaseException
    {
        /// <summary>
        /// 判题异常
        /// </summary>
        /// <param name="message">Message</param>
        public JudgeException(string message) : base(message)
        { }
    }
}