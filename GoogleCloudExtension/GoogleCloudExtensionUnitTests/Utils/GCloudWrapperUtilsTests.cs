// Copyright 2016 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.Utils
{
    [TestClass]
    public class GCloudWrapperUtilsTests
    {
        private static readonly string s_missingComponentTitle = Resources.GcloudMissingComponentTitle;
        private static readonly string s_missingComponentMessage = string.Format(Resources.GcloudMissingComponentErrorMessage, "Kubectl");
        private static readonly string s_missingComponentInstallCommand = string.Format(Resources.GcloudMissingComponentInstallCommand, "kubectl").ToLower();

        private Mock<Func<GCloudComponent, Task<GCloudValidationResult>>> _validateGCloudAsyncMock;
        private TaskCompletionSource<GCloudValidationResult> _validateGCloudAsyncSource;
        private Mock<Action<string, string, string>> _showCopyablePromptMock;

        [TestInitialize]
        public void BeforeEach()
        {
            _validateGCloudAsyncSource = new TaskCompletionSource<GCloudValidationResult>();
            _validateGCloudAsyncMock = new Mock<Func<GCloudComponent, Task<GCloudValidationResult>>>();
            _validateGCloudAsyncMock.Setup(f => f(It.IsAny<GCloudComponent>())).Returns(_validateGCloudAsyncSource.Task);

            _showCopyablePromptMock = new Mock<Action<string, string, string>>();

            GCloudWrapperUtils.ValidateGCloudAsyncOverride = _validateGCloudAsyncMock.Object;
            GCloudWrapperUtils.ShowCopyablePromptOverride = _showCopyablePromptMock.Object;

        }

        [TestCleanup]
        public void AfterEach()
        {
            GCloudWrapperUtils.ValidateGCloudAsyncOverride = null;
            GCloudWrapperUtils.ShowCopyablePromptOverride = null;
        }

        [TestMethod]
        public async Task TestMissingComponentPrompt()
        {
            _validateGCloudAsyncSource.SetResult(GCloudValidationResult.MissingComponent);

            _showCopyablePromptMock
                .Setup(a => a(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((title, message, copyableMessage) =>
                {
                    Assert.AreEqual(s_missingComponentTitle, title);
                    Assert.AreEqual(s_missingComponentMessage, message);
                    Assert.AreEqual(s_missingComponentInstallCommand, copyableMessage, false);
                });

            await GCloudWrapperUtils.VerifyGCloudDependenciesAsync(GCloudComponent.Kubectl);

            _showCopyablePromptMock.Verify(a => a(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}
