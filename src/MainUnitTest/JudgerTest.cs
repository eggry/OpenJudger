﻿using System;
using Xunit;
using Judger.Core.Program;
using Judger.Core.Program.Entity;

namespace MainUnitTest
{
    public class JudgerTest
    {
        [Fact]
        public void TestSingleJudgerCompare()
        {
            SingleCaseJudger judger = new SingleCaseJudger(new Judger.Entity.JudgeTask());

            string test1 = @"123123";
            string test2 = @"123123";
            Assert.True(judger.CompareAnswer(test1, test2) == CompareResult.Accepted);

            test1 = "123\r\n123";
            test2 = "123\n123";
            Assert.True(judger.CompareAnswer(test1, test2) == CompareResult.Accepted);

            test1 = "123\r\n123\r\n";
            test2 = "123\n123";
            Assert.True(judger.CompareAnswer(test1, test2) == CompareResult.Accepted);

            test1 = "123\r\n123\r\n";
            test2 = "123 \n123";
            Assert.True(judger.CompareAnswer(test1, test2) == CompareResult.PresentationError);

            test1 = "123123\n\n\r\n";
            test2 = "123123";
            Assert.True(judger.CompareAnswer(test1, test2) == CompareResult.Accepted);

            test1 = "123\n123\n\n\r\n";
            test2 = "\r\r\n\r\n 123\n123";
            Assert.True(judger.CompareAnswer(test1, test2) == CompareResult.PresentationError);

            test1 = "123\n123\n";
            test2 = "1232\n123\n";
            Assert.True(judger.CompareAnswer(test1, test2) == CompareResult.WrongAnswer);
        }
    }
}
