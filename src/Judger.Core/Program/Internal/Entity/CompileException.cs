﻿using Judger.Entity;

namespace Judger.Core.Program.Internal.Entity
{
    public class CompileException : JudgeException
    {
        public CompileException(string message) : base(message)
        { }
    }
}