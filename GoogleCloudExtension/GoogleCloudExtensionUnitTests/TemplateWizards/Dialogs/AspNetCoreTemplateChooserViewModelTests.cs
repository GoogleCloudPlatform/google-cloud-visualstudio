// Copyright 2018 Google Inc. All Rights Reserved.
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


using Google.Apis.CloudResourceManager.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.TemplateWizards.Dialogs.TemplateChooserDialog;
using GoogleCloudExtension.VsVersion;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GoogleCloudExtensionUnitTests.TemplateWizards.Dialogs
{
    [TestClass]
    public class AspNetCoreTemplateChooserViewModelTests : ExtensionTestBase
    {
        private const string DefaultProjectId = "default-project-id";
        private List<string> _targetSdkVersions;
        private Mock<Action> _closeWindowMock;

        protected override void BeforeEach()
        {
            _targetSdkVersions = new List<string>();
            // ReSharper disable once PossibleUnintendedReferenceComparison
            VsVersionUtils.s_toolsPathProvider = new Lazy<IToolsPathProvider>(
                () => Mock.Of<IToolsPathProvider>(tpp => tpp.GetNetCoreSdkVersions() == _targetSdkVersions));
            CredentialsStore.Default.UpdateCurrentProject(Mock.Of<Project>(p => p.ProjectId == DefaultProjectId));
            _closeWindowMock = new Mock<Action>();
            PackageMock.Setup(p => p.VsVersion).Returns(VsVersionUtils.VisualStudio2017Version);
        }

        [TestMethod]
        public void TestInitialConditionsVs2015MissingNetCoreSdk()
        {
            PackageMock.Setup(p => p.VsVersion).Returns(VsVersionUtils.VisualStudio2015Version);

            var objectUnderTest = new AspNetCoreTemplateChooserViewModel(_closeWindowMock.Object);

            Assert.IsTrue(objectUnderTest.NetCoreMissingError);
            Assert.IsFalse(objectUnderTest.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestInitialConditionsVs2017MissingNetCoreSdk()
        {
            PackageMock.Setup(p => p.VsVersion).Returns(VsVersionUtils.VisualStudio2017Version);

            var objectUnderTest = new AspNetCoreTemplateChooserViewModel(_closeWindowMock.Object);

            CollectionAssert.AreEqual(new[] { FrameworkType.NetFramework }, objectUnderTest.AvailableFrameworks.ToList());
            Assert.AreEqual(FrameworkType.NetFramework, objectUnderTest.SelectedFramework);
            CollectionAssert.AreEqual(
                new[] { AspNetVersion.AspNetCore10, AspNetVersion.AspNetCore11, AspNetVersion.AspNetCore20 },
                objectUnderTest.AvailableVersions.ToList());
            Assert.AreEqual(AspNetVersion.AspNetCore10, objectUnderTest.SelectedVersion);
        }

        [TestMethod]
        public void TestInitialConditionsVs2015()
        {
            PackageMock.Setup(p => p.VsVersion).Returns(VsVersionUtils.VisualStudio2015Version);
            _targetSdkVersions.Add("1.0.0-preview2-003156");

            var objectUnderTest = new AspNetCoreTemplateChooserViewModel(_closeWindowMock.Object);

            CollectionAssert.AreEqual(new[] { FrameworkType.NetCore, FrameworkType.NetFramework }, objectUnderTest.AvailableFrameworks.ToList());
            Assert.AreEqual(FrameworkType.NetCore, objectUnderTest.SelectedFramework);
            CollectionAssert.AreEqual(
                new[] { AspNetVersion.AspNetCore1Preview }, objectUnderTest.AvailableVersions.ToList());
            Assert.AreEqual(AspNetVersion.AspNetCore1Preview, objectUnderTest.SelectedVersion);
        }

        [TestMethod]
        public void TestInitialConditionsVs2017WithNetCoreSdk10()
        {
            PackageMock.Setup(p => p.VsVersion).Returns(VsVersionUtils.VisualStudio2017Version);
            _targetSdkVersions.Add(AspNetVersion.s_firstSdkVersionWith11Runtime.ToString());

            var objectUnderTest = new AspNetCoreTemplateChooserViewModel(_closeWindowMock.Object);

            CollectionAssert.AreEqual(
                new[] { FrameworkType.NetCore, FrameworkType.NetFramework },
                objectUnderTest.AvailableFrameworks.ToList());
            Assert.AreEqual(FrameworkType.NetCore, objectUnderTest.SelectedFramework);
            CollectionAssert.AreEqual(
                new[] { AspNetVersion.AspNetCore10, AspNetVersion.AspNetCore11 },
                objectUnderTest.AvailableVersions.ToList());
            Assert.AreEqual(AspNetVersion.AspNetCore10, objectUnderTest.SelectedVersion);
        }

        [TestMethod]
        public void TestInitialConditionsVs2017WithNetCoreSdk20()
        {
            PackageMock.Setup(p => p.VsVersion).Returns(VsVersionUtils.VisualStudio2017Version);
            _targetSdkVersions.Add("2.0.0");

            var objectUnderTest = new AspNetCoreTemplateChooserViewModel(_closeWindowMock.Object);

            CollectionAssert.AreEqual(
                new[] { FrameworkType.NetCore, FrameworkType.NetFramework },
                objectUnderTest.AvailableFrameworks.ToList());
            Assert.AreEqual(FrameworkType.NetCore, objectUnderTest.SelectedFramework);
            CollectionAssert.AreEqual(
                new[] { AspNetVersion.AspNetCore20 },
                objectUnderTest.AvailableVersions.ToList());
            Assert.AreEqual(AspNetVersion.AspNetCore20, objectUnderTest.SelectedVersion);
        }

        [TestMethod]
        public void TestInitialConditionsVs2017WithBothNetCoreSdk10And20()
        {
            PackageMock.Setup(p => p.VsVersion).Returns(VsVersionUtils.VisualStudio2017Version);
            _targetSdkVersions.Add("2.0.0");
            _targetSdkVersions.Add(AspNetVersion.s_firstSdkVersionWith11Runtime.ToString());

            var objectUnderTest = new AspNetCoreTemplateChooserViewModel(_closeWindowMock.Object);

            CollectionAssert.AreEqual(
                new[] { FrameworkType.NetCore, FrameworkType.NetFramework },
                objectUnderTest.AvailableFrameworks.ToList());
            Assert.AreEqual(FrameworkType.NetCore, objectUnderTest.SelectedFramework);
            CollectionAssert.AreEqual(
                new[] { AspNetVersion.AspNetCore10, AspNetVersion.AspNetCore11, AspNetVersion.AspNetCore20 },
                objectUnderTest.AvailableVersions.ToList());
            Assert.AreEqual(AspNetVersion.AspNetCore10, objectUnderTest.SelectedVersion);
        }

        [TestMethod]
        public void TestSetSelectedVersion()
        {
            var objectUnderTest = new AspNetCoreTemplateChooserViewModel(_closeWindowMock.Object);

            objectUnderTest.SelectedVersion = AspNetVersion.AspNetCore11;

            Assert.AreEqual(AspNetVersion.AspNetCore11, objectUnderTest.SelectedVersion);
        }

        [TestMethod]
        public void TestSetSelectedFrameworkKeepSelectedVersion()
        {
            _targetSdkVersions.Add("2.0.0");
            _targetSdkVersions.Add(AspNetVersion.s_firstSdkVersionWith11Runtime.ToString());
            var objectUnderTest = new AspNetCoreTemplateChooserViewModel(_closeWindowMock.Object);
            objectUnderTest.SelectedFramework = FrameworkType.NetFramework;

            objectUnderTest.SelectedVersion = AspNetVersion.AspNetCore11;
            objectUnderTest.SelectedFramework = FrameworkType.NetCore;

            Assert.AreEqual(FrameworkType.NetCore, objectUnderTest.SelectedFramework);
            CollectionAssert.AreEqual(
                new[]
                {
                    AspNetVersion.AspNetCore10,
                    AspNetVersion.AspNetCore11,
                    AspNetVersion.AspNetCore20
                }, objectUnderTest.AvailableVersions.ToList());
            Assert.AreEqual(AspNetVersion.AspNetCore11, objectUnderTest.SelectedVersion);
        }

        [TestMethod]
        public void TestCreateResult()
        {
            const string resultProjectId = "result-project-id";
            AspNetVersion resultVersion = AspNetVersion.AspNetCore11;
            const FrameworkType resultFrameworkType = FrameworkType.NetCore;
            _targetSdkVersions.Add("2.0.0");
            _targetSdkVersions.Add("1.0.0");

            var objectUnderTest = new AspNetCoreTemplateChooserViewModel(_closeWindowMock.Object)
            {
                SelectedFramework = resultFrameworkType,
                SelectedVersion = resultVersion,
                GcpProjectId = resultProjectId,
                IsWebApi = true
            };

            objectUnderTest.OkCommand.Execute(null);

            Assert.IsNotNull(objectUnderTest.Result);
            Assert.AreEqual(resultProjectId, objectUnderTest.Result.GcpProjectId);
            Assert.AreEqual(resultFrameworkType, objectUnderTest.Result.SelectedFramework);
            Assert.AreEqual(resultVersion, objectUnderTest.Result.SelectedVersion);
            Assert.AreEqual(AppType.WebApi, objectUnderTest.Result.AppType);
        }

        [TestMethod]
        public void TestOpenVisualStudio2015DotNetCoreToolingDownloadLink()
        {

            var objectUnderTest = new AspNetCoreTemplateChooserViewModel(_closeWindowMock.Object);
            var startProcessMock = new Mock<Func<string, Process>>();
            AspNetCoreTemplateChooserViewModel.StartProcessOverride = startProcessMock.Object;

            objectUnderTest.OpenVisualStudio2015DotNetCoreToolingDownloadLink.Execute(null);

            startProcessMock.Verify(
                f => f(AspNetCoreTemplateChooserViewModel.DotnetCoreDownloadArchiveUrl), Times.Once);
        }
    }
}
