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

using Google.Apis.CloudResourceManager.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.TemplateWizards.Dialogs.TemplateChooserDialog;
using GoogleCloudExtension.VsVersion;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleCloudExtensionUnitTests.TemplateWizards.Dialogs
{
    /// <summary>
    /// Class for testing <see cref="TemplateChooserViewModel"/>.
    /// </summary>
    [TestClass]
    public class TemplateChooserViewModelTests
    {
        private const string DefaultProjectId = "default-project-id";
        private Mock<Action> _closeWindowMock;
        private Mock<Func<string>> _promptPickProjectMock;
        private TemplateChooserViewModel _objectUnderTest;
        private List<string> _targetSdkVersions;

        [TestInitialize]
        public void BeforeEach()
        {
            _targetSdkVersions = new List<string>();
            VsVersionUtils.s_toolsPathProvider = new Lazy<IToolsPathProvider>(
                () => Mock.Of<IToolsPathProvider>(tpp => tpp.GetNetCoreSdkVersions() == _targetSdkVersions));
            CredentialsStore.Default.UpdateCurrentProject(Mock.Of<Project>(p => p.ProjectId == DefaultProjectId));
            _closeWindowMock = new Mock<Action>();
            _promptPickProjectMock = new Mock<Func<string>>();
            GoogleCloudExtensionPackageTests.InitPackageMock(
                dteMock => dteMock.Setup(dte => dte.Version).Returns(VsVersionUtils.VisualStudio2017Version));
            _objectUnderTest = new TemplateChooserViewModel(_closeWindowMock.Object, _promptPickProjectMock.Object);
        }

        [TestMethod]
        public void TestInitialConditions()
        {
            Assert.AreEqual(AppType.Mvc, _objectUnderTest.AppType);
            Assert.AreEqual(true, _objectUnderTest.OkCommand.CanExecuteCommand);
            Assert.AreEqual(DefaultProjectId, _objectUnderTest.GcpProjectId);
            Assert.IsNull(_objectUnderTest.Result);
        }

        [TestMethod]
        public void TestInitialConditionsVs2015NoNetCoreSdk()
        {
            GoogleCloudExtensionPackageTests.InitPackageMock(
                dteMock => dteMock.Setup(dte => dte.Version).Returns(VsVersionUtils.VisualStudio2015Version));

            _objectUnderTest = new TemplateChooserViewModel(_closeWindowMock.Object, _promptPickProjectMock.Object);

            Assert.IsFalse(_objectUnderTest.NetCoreAvailable);
            Assert.AreEqual(FrameworkType.NetFramework, _objectUnderTest.SelectedFramework);
            CollectionAssert.AreEqual(
                new[] { AspNetVersion.AspNet4 }, _objectUnderTest.AvailableVersions.ToList());
            Assert.AreEqual(AspNetVersion.AspNet4, _objectUnderTest.SelectedVersion);
        }

        [TestMethod]
        public void TestInitialConditionsVs2015()
        {
            GoogleCloudExtensionPackageTests.InitPackageMock(
                dteMock => dteMock.Setup(dte => dte.Version).Returns(VsVersionUtils.VisualStudio2015Version));
            _targetSdkVersions.Add("1.0.0-preview2-003156");

            _objectUnderTest = new TemplateChooserViewModel(_closeWindowMock.Object, _promptPickProjectMock.Object);

            Assert.IsTrue(_objectUnderTest.NetCoreAvailable);
            Assert.AreEqual(FrameworkType.NetCore, _objectUnderTest.SelectedFramework);
            CollectionAssert.AreEqual(
                new[] { AspNetVersion.AspNetCore1Preview }, _objectUnderTest.AvailableVersions.ToList());
            Assert.AreEqual(AspNetVersion.AspNetCore1Preview, _objectUnderTest.SelectedVersion);
        }

        [TestMethod]
        public void TestInitialConditionsVs2017WithNoNetCoreSdk()
        {
            GoogleCloudExtensionPackageTests.InitPackageMock(
                dteMock => dteMock.Setup(dte => dte.Version).Returns(VsVersionUtils.VisualStudio2017Version));

            _objectUnderTest = new TemplateChooserViewModel(_closeWindowMock.Object, _promptPickProjectMock.Object);

            Assert.IsFalse(_objectUnderTest.NetCoreAvailable);
            Assert.AreEqual(FrameworkType.NetFramework, _objectUnderTest.SelectedFramework);
            CollectionAssert.AreEqual(
                new[] { AspNetVersion.AspNet4 },
                _objectUnderTest.AvailableVersions.ToList());
            Assert.AreEqual(AspNetVersion.AspNet4, _objectUnderTest.SelectedVersion);
        }

        [TestMethod]
        public void TestInitialConditionsVs2017WithNetCoreSdk20()
        {
            GoogleCloudExtensionPackageTests.InitPackageMock(
                dteMock => dteMock.Setup(dte => dte.Version).Returns(VsVersionUtils.VisualStudio2017Version));
            _targetSdkVersions.Add("2.0.0");

            _objectUnderTest = new TemplateChooserViewModel(_closeWindowMock.Object, _promptPickProjectMock.Object);

            Assert.IsTrue(_objectUnderTest.NetCoreAvailable);
            Assert.AreEqual(FrameworkType.NetCore, _objectUnderTest.SelectedFramework);
            CollectionAssert.AreEqual(
                new[] { AspNetVersion.AspNetCore20 },
                _objectUnderTest.AvailableVersions.ToList());
            Assert.AreEqual(AspNetVersion.AspNetCore20, _objectUnderTest.SelectedVersion);
        }

        [TestMethod]
        public void TestInitialConditionsVs2017WithNetCoreSdk10()
        {
            GoogleCloudExtensionPackageTests.InitPackageMock(
                dteMock => dteMock.Setup(dte => dte.Version).Returns(VsVersionUtils.VisualStudio2017Version));
            _targetSdkVersions.Add("1.0.0");

            _objectUnderTest = new TemplateChooserViewModel(_closeWindowMock.Object, _promptPickProjectMock.Object);

            Assert.IsTrue(_objectUnderTest.NetCoreAvailable);
            Assert.AreEqual(FrameworkType.NetCore, _objectUnderTest.SelectedFramework);
            CollectionAssert.AreEqual(
                new[] { AspNetVersion.AspNetCore10, AspNetVersion.AspNetCore11 },
                _objectUnderTest.AvailableVersions.ToList());
            Assert.AreEqual(AspNetVersion.AspNetCore10, _objectUnderTest.SelectedVersion);
        }

        [TestMethod]
        public void TestInitialConditionsVs2017WithBothNetCoreSdk10And20()
        {
            GoogleCloudExtensionPackageTests.InitPackageMock(
                dteMock => dteMock.Setup(dte => dte.Version).Returns(VsVersionUtils.VisualStudio2017Version));
            _targetSdkVersions.Add("2.0.0");
            _targetSdkVersions.Add("1.0.0");

            _objectUnderTest = new TemplateChooserViewModel(_closeWindowMock.Object, _promptPickProjectMock.Object);

            Assert.IsTrue(_objectUnderTest.NetCoreAvailable);
            Assert.AreEqual(FrameworkType.NetCore, _objectUnderTest.SelectedFramework);
            CollectionAssert.AreEqual(
                new[] { AspNetVersion.AspNetCore10, AspNetVersion.AspNetCore11, AspNetVersion.AspNetCore20 },
                _objectUnderTest.AvailableVersions.ToList());
            Assert.AreEqual(AspNetVersion.AspNetCore10, _objectUnderTest.SelectedVersion);
        }

        [TestMethod]
        public void TestSetMvc()
        {
            _objectUnderTest.IsMvc = true;

            Assert.IsTrue(_objectUnderTest.IsMvc);
            Assert.IsFalse(_objectUnderTest.IsWebApi);
            Assert.AreEqual(AppType.Mvc, _objectUnderTest.AppType);
            Assert.IsTrue(_objectUnderTest.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestChangeToMvc()
        {
            _objectUnderTest.IsWebApi = true;

            _objectUnderTest.IsMvc = true;

            Assert.IsTrue(_objectUnderTest.IsMvc);
            Assert.IsFalse(_objectUnderTest.IsWebApi);
            Assert.AreEqual(AppType.Mvc, _objectUnderTest.AppType);
            Assert.IsTrue(_objectUnderTest.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestUnsetMvc()
        {
            _objectUnderTest.IsMvc = true;

            _objectUnderTest.IsMvc = false;

            Assert.IsFalse(_objectUnderTest.IsMvc);
            Assert.IsFalse(_objectUnderTest.IsWebApi);
            Assert.AreEqual(AppType.None, _objectUnderTest.AppType);
            Assert.IsFalse(_objectUnderTest.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestSetWebApi()
        {
            _objectUnderTest.IsWebApi = true;

            Assert.IsFalse(_objectUnderTest.IsMvc);
            Assert.IsTrue(_objectUnderTest.IsWebApi);
            Assert.AreEqual(AppType.WebApi, _objectUnderTest.AppType);
            Assert.IsTrue(_objectUnderTest.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestChangeToWebApi()
        {
            _objectUnderTest.IsMvc = true;

            _objectUnderTest.IsWebApi = true;

            Assert.IsFalse(_objectUnderTest.IsMvc);
            Assert.IsTrue(_objectUnderTest.IsWebApi);
            Assert.AreEqual(AppType.WebApi, _objectUnderTest.AppType);
            Assert.IsTrue(_objectUnderTest.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestUnsetWebApi()
        {
            _objectUnderTest.IsWebApi = true;

            _objectUnderTest.IsWebApi = false;

            Assert.IsFalse(_objectUnderTest.IsMvc);
            Assert.IsFalse(_objectUnderTest.IsWebApi);
            Assert.AreEqual(AppType.None, _objectUnderTest.AppType);
            Assert.IsFalse(_objectUnderTest.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestSetSelectedVersion()
        {
            _objectUnderTest = new TemplateChooserViewModel(_closeWindowMock.Object, _promptPickProjectMock.Object);

            _objectUnderTest.SelectedVersion = AspNetVersion.AspNetCore11;

            Assert.AreEqual(AspNetVersion.AspNetCore11, _objectUnderTest.SelectedVersion);
        }

        [TestMethod]
        public void TestSetSelectedFrameworkKeepSelectedVersion()
        {
            _targetSdkVersions.Add("2.0.0");
            _targetSdkVersions.Add("1.0.0");
            _objectUnderTest = new TemplateChooserViewModel(_closeWindowMock.Object, _promptPickProjectMock.Object);

            _objectUnderTest.SelectedVersion = AspNetVersion.AspNetCore11;
            _objectUnderTest.SelectedFramework = FrameworkType.NetCore;

            Assert.AreEqual(FrameworkType.NetCore, _objectUnderTest.SelectedFramework);
            CollectionAssert.AreEqual(
                new[]
                {
                    AspNetVersion.AspNetCore10, AspNetVersion.AspNetCore11, AspNetVersion.AspNetCore20
                }, _objectUnderTest.AvailableVersions.ToList());
            Assert.AreEqual(AspNetVersion.AspNetCore11, _objectUnderTest.SelectedVersion);
        }

        [TestMethod]
        public void TestSetSelectedFrameworkInvalidateSelectedVersion()
        {
            _targetSdkVersions.Add("2.0.0");
            _targetSdkVersions.Add("1.0.0");
            _objectUnderTest = new TemplateChooserViewModel(_closeWindowMock.Object, _promptPickProjectMock.Object);

            _objectUnderTest.SelectedFramework = FrameworkType.NetFramework;
            _objectUnderTest.SelectedVersion = AspNetVersion.AspNet4;
            _objectUnderTest.SelectedFramework = FrameworkType.NetCore;

            Assert.AreEqual(FrameworkType.NetCore, _objectUnderTest.SelectedFramework);
            CollectionAssert.AreEqual(
                new[]
                {
                    AspNetVersion.AspNetCore10, AspNetVersion.AspNetCore11, AspNetVersion.AspNetCore20
                }, _objectUnderTest.AvailableVersions.ToList());
            Assert.AreEqual(AspNetVersion.AspNetCore10, _objectUnderTest.SelectedVersion);
        }

        [TestMethod]
        public void TestSelectProject()
        {
            const string mockProjectID = "mock-project-id";
            _promptPickProjectMock.Setup(f => f()).Returns(mockProjectID);

            _objectUnderTest.SelectProjectCommand.Execute(null);

            Assert.AreEqual(mockProjectID, _objectUnderTest.GcpProjectId);
        }

        [TestMethod]
        public void TestSelectProjectCanceled()
        {
            _promptPickProjectMock.Setup(f => f()).Returns((string)null);

            _objectUnderTest.SelectProjectCommand.Execute(null);

            Assert.AreEqual(DefaultProjectId, _objectUnderTest.GcpProjectId);
        }

        [TestMethod]
        public void TestOkCommand()
        {
            _objectUnderTest.OkCommand.Execute(null);

            _closeWindowMock.Verify(f => f(), Times.Once);
            Assert.IsNotNull(_objectUnderTest.Result);
            Assert.AreEqual(_objectUnderTest.AppType, _objectUnderTest.Result.AppType);
            Assert.AreEqual(_objectUnderTest.GcpProjectId, _objectUnderTest.Result.GcpProjectId);
            Assert.AreEqual(_objectUnderTest.SelectedFramework, _objectUnderTest.Result.SelectedFramework);
            Assert.AreEqual(_objectUnderTest.SelectedVersion, _objectUnderTest.Result.SelectedVersion);
        }
    }
}
