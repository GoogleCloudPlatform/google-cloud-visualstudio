﻿// Copyright 2017 Google Inc. All Rights Reserved.
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

using static GoogleCloudExtension.Utils.PathUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace GoogleCloudExtension.Utils.UnitTests
{
    [TestClass]
    public class PathUtilsTests
    {
        [TestMethod]
        public void GetCommandPathFromPATHTest()
        {
            var path = Environment.GetEnvironmentVariable("PATH");
            path += ";+||+++";
            Environment.SetEnvironmentVariable("PATH", path);

            var notepad = GetCommandPathFromPATH("notepad.exe");
            Assert.IsNotNull(notepad);
            Assert.IsTrue(notepad.Contains("notepad.exe"));

            // This verifies GetCommandPathFromPATH does not throw exception.
            var testpath = GetCommandPathFromPATH("does-not-exist-such.exe");
            Assert.IsNull(testpath);
        }
    }
}