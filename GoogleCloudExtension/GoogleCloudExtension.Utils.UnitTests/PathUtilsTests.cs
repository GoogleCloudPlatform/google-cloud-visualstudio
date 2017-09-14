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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using static GoogleCloudExtension.Utils.PathUtils;

namespace GoogleCloudExtension.Utils.UnitTests
{
    [TestClass]
    public class PathUtilsTests
    {
        [TestMethod]
        public void GetCommandPathFromPATHValidPathTest()
        {
            var notepad = GetCommandPathFromPATH("notepad.exe");
            Assert.IsNotNull(notepad);
            Assert.IsTrue(notepad.Contains("notepad.exe"));
        }

        [TestMethod]
        public void GetCommandPathFromPATHInvalidPathTest()
        {
            var path = Environment.GetEnvironmentVariable("PATH");
            try
            {
                // Insert an invalid path that throws ArgumentException when calling Path.Combine
                var modifiedPath = path + ";+||+++";
                Environment.SetEnvironmentVariable("PATH", modifiedPath);

                // This verifies GetCommandPathFromPATH does not throw exception.
                var testPath = GetCommandPathFromPATH("does-not-exist-such.exe");
                Assert.IsNull(testPath);
            }
            finally
            {
                Environment.SetEnvironmentVariable("PATH", path);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestEnsureEndSeparatorNull()
        {
            PathUtils.EnsureEndSeparator(null);
        }

        [TestMethod]
        public void TestEnsureEndSeparatorEndBackslash()
        {
            const string input = @"root:\\Directory\\";

            string result = input.EnsureEndSeparator();

            Assert.AreEqual(input, result);
        }

        [TestMethod]
        public void TestEnsureEndSeparatorEndSlash()
        {
            const string input = "root:/Directory/";

            string result = input.EnsureEndSeparator();

            Assert.AreEqual(input, result);
        }

        [TestMethod]
        public void TestEnsureEndSeparatorMissingBackslash()
        {
            const string input = @"root:\Directory";

            string result = input.EnsureEndSeparator();

            Assert.AreEqual(input + @"\", result);
        }

        [TestMethod]
        public void TestEnsureEndSeparatorMissingSlash()
        {
            const string input = "root:/Directory";

            string result = input.EnsureEndSeparator();

            Assert.AreEqual(input + @"\", result);
        }

        [TestMethod]
        public void TestGetRelativePathSame()
        {
            const string fromDir = @"root:\Directory";

            string result1 = GetRelativePath(fromDir, fromDir);
            string result2 = GetRelativePath(fromDir.Replace('\\', '/'), fromDir);
            string result3 = GetRelativePath(fromDir, fromDir.Replace('\\', '/'));
            string result4 = GetRelativePath(fromDir.Replace('\\', '/'), fromDir.Replace('\\', '/'));

            const string expected = "";
            Assert.AreEqual(expected, result1);
            Assert.AreEqual(expected, result2);
            Assert.AreEqual(expected, result3);
            Assert.AreEqual(expected, result4);
        }

        [TestMethod]
        public void TestGetRelativePathToParent()
        {
            const string fromDir = @"root:\BaseDir\Directory";
            const string toDir = @"root:\BaseDir";

            string result1 = GetRelativePath(fromDir, toDir);
            string result2 = GetRelativePath(fromDir.Replace('\\', '/'), toDir);
            string result3 = GetRelativePath(fromDir, toDir.Replace('\\', '/'));
            string result4 = GetRelativePath(fromDir.Replace('\\', '/'), toDir.Replace('\\', '/'));

            const string expected = @"..\";
            Assert.AreEqual(expected, result1);
            Assert.AreEqual(expected, result2);
            Assert.AreEqual(expected, result3);
            Assert.AreEqual(expected, result4);
        }

        [TestMethod]
        public void TestGetRelativePathToChild()
        {
            const string fromDir = @"root:\BaseDir";
            const string toDir = @"root:\BaseDir\Directory";


            string result1 = GetRelativePath(fromDir, toDir);
            string result2 = GetRelativePath(fromDir.Replace('\\', '/'), toDir);
            string result3 = GetRelativePath(fromDir, toDir.Replace('\\', '/'));
            string result4 = GetRelativePath(fromDir.Replace('\\', '/'), toDir.Replace('\\', '/'));

            const string expected = @"Directory\";
            Assert.AreEqual(expected, result1);
            Assert.AreEqual(expected, result2);
            Assert.AreEqual(expected, result3);
            Assert.AreEqual(expected, result4);
        }

        [TestMethod]
        public void TestGetRelativePathCousin()
        {
            const string fromDir = @"root:\BaseDir\SomeDir";
            const string toDir = @"root:\BaseDir\OtherDir";

            string result1 = GetRelativePath(fromDir, toDir);
            string result2 = GetRelativePath(fromDir.Replace('\\', '/'), toDir);
            string result3 = GetRelativePath(fromDir, toDir.Replace('\\', '/'));
            string result4 = GetRelativePath(fromDir.Replace('\\', '/'), toDir.Replace('\\', '/'));

            const string expected = @"..\OtherDir\";
            Assert.AreEqual(expected, result1);
            Assert.AreEqual(expected, result2);
            Assert.AreEqual(expected, result3);
            Assert.AreEqual(expected, result4);
        }
    }
}
