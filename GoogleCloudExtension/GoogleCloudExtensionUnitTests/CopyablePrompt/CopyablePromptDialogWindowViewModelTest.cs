﻿// Copyright 2016 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.CopyablePrompt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;

namespace GoogleCloudExtensionUnitTests.CopyablePrompt
{
    [TestClass]
    public class CopyablePromptDialogWindowViewModelTest
    {
        private const string Message = "Message that cannot be copied";
        private const string CopyableText = "Text that can be copied";

        [TestMethod]
        public void TestConstructor()
        {
            var objectUnderTest = new CopyablePromptDialogWindowViewModel(Message, CopyableText);

            Assert.AreEqual(Message, objectUnderTest.Message);
            Assert.AreEqual(CopyableText, objectUnderTest.CopyableText);
        }

        [TestMethod]
        public void TestCopyCommand()
        {
            var objectUnderTest = new CopyablePromptDialogWindowViewModel(Message, CopyableText);

            objectUnderTest.CopyCommand.Execute(null);

            Assert.AreEqual(CopyableText, Clipboard.GetText());
        }
    }
}
