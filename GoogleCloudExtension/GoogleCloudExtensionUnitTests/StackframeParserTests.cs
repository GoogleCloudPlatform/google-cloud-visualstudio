// Copyright 2017 Google Inc. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using GoogleCloudExtension.StackdriverErrorReporting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GoogleCloudExtensionUnitTests
{
    [TestClass]
    public class StackframeParserTests
    {
        private const string ExceptionTestMessage = "parser test message";
        private static readonly string s_exceptionHeader = $"{typeof(InvalidOperationException).FullName}: {ExceptionTestMessage}";

        [TestMethod]
        public void SimpleException()
        {
            var exception = GenerateException(() => Loop(3));
            var stackTrace = new StackTrace(exception, fNeedFileInfo: true);
            var parsedException = new ParsedException(exception.ToString());
            Assert.AreEqual(stackTrace.FrameCount, parsedException.StackFrames.Where(x => x.IsWellParsed).Count());
            Assert.AreEqual(s_exceptionHeader, parsedException.Header);
        }

        [TestMethod]
        public void InterException()
        {
            var exception = GenerateException(() => GenerateInnerException(3));
            var parsedException = new ParsedException(exception.ToString());
            Assert.AreEqual(14, parsedException.StackFrames.Where(x => x.IsWellParsed).Count());
        }

        [TestMethod]
        public void OptimizedCode()
        {
            var exception = GenerateException(() => File.Create(@"file://kkk\..\..\..\this is invalid path"));
            var parsedException = new ParsedException(exception.ToString());
            Assert.AreEqual(2, parsedException.StackFrames.Where(x => x.IsWellParsed).Count());
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private static Exception GenerateException(Action act)
        {
            try
            {
                act();
                throw new NotImplementedException();
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private static void Loop(int count)
        {
            if (count > 0)
            {
                Loop(--count);
            }
            else
            {
                throw new InvalidOperationException(ExceptionTestMessage);
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private static void GenerateInnerException(int count)
        {
            if (count > 0)
            {
                GenerateInnerException(--count);
            }
            else
            {
                throw new InvalidOperationException(ExceptionTestMessage, GenerateException(() => Loop(5)));
            }
        }
    }
}
