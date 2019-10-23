﻿namespace Judger.Models.Exception
{
    /// <summary>
    /// Fetcher异常
    /// </summary>
    public class FetcherException : BaseException
    {
        /// <summary>
        /// Fetcher异常
        /// </summary>
        /// <param name="message">Message</param>
        public FetcherException(string message) : base(message)
        { }
    }
}