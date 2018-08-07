﻿// Copyright 2018 Google Inc. All Rights Reserved.
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
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialog.Steps;
using GoogleCloudExtension.PublishDialog.Steps.Choice;
using GoogleCloudExtension.PublishDialog.Steps.CoreGceWarning;
using GoogleCloudExtension.PublishDialog.Steps.Gce;
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Services.VsProject;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TestingHelpers;
using Project = EnvDTE.Project;

namespace GoogleCloudExtensionUnitTests.PublishDialog.Steps.CoreGceWarning
{
    [TestClass]
    public class CoreGceWarningStepViewModelTests : ExtensionTestBase
    {
        private Mock<IPublishDialog> _publishDialogMock;
        private Mock<IVsProjectPropertyService> _propertyServiceMock;
        private CoreGceWarningStepViewModel _objectUnderTest;
        private Project _dteProject;
        private Mock<IBrowserService> _browserServiceMock;
        private const string VisualStudioProjectName = "VisualStudioProjectname";

        [TestInitialize]
        public void BeforeEach()
        {
            _dteProject = Mock.Of<Project>();
            _publishDialogMock = new Mock<IPublishDialog>();
            _publishDialogMock.Setup(pd => pd.Project.Name).Returns(VisualStudioProjectName);
            _publishDialogMock.Setup(pd => pd.Project.ProjectType).Returns(KnownProjectTypes.NetCoreWebApplication);
            _publishDialogMock.Setup(pd => pd.Project.Project).Returns(_dteProject);

            _propertyServiceMock = new Mock<IVsProjectPropertyService>();
            _browserServiceMock = new Mock<IBrowserService>();
            PackageMock.Setup(p => p.GetMefService<IVsProjectPropertyService>()).Returns(_propertyServiceMock.Object);
            PackageMock.Setup(p => p.GetMefServiceLazy<IBrowserService>()).Returns(_browserServiceMock.ToLazy());

            _objectUnderTest = new CoreGceWarningStepViewModel(_publishDialogMock.Object);
        }

        [TestMethod]
        public void TestConstructor_SetsTitle()
        {
            const string expectedName = "ExpectedName";

            _objectUnderTest =
                new CoreGceWarningStepViewModel(Mock.Of<IPublishDialog>(pd => pd.Project.Name == expectedName));

            StringAssert.Contains(_objectUnderTest.Title, expectedName);
        }

        [TestMethod]
        public void TestConstructor_SetsPublishCaption()
        {
            _objectUnderTest = new CoreGceWarningStepViewModel(_publishDialogMock.Object);

            Assert.AreEqual(Resources.PublishDialogNextButtonCaption, _objectUnderTest.ActionCaption);
        }

        [TestMethod]
        public void TestPublishCommand_SavesChoiceProperty()
        {
            _objectUnderTest.ActionCommand.Execute(null);

            _propertyServiceMock.Verify(
                ps => ps.SaveUserProperty(
                    _dteProject,
                    ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName,
                    ChoiceType.Gce.ToString()));
        }

        [TestMethod]
        public void TestPublishCommand_NavigatesToNextStep()
        {
            _objectUnderTest.ActionCommand.Execute(null);

            _publishDialogMock.Verify(pd => pd.NavigateToStep(It.IsAny<GceStepContent>()));
        }

        [TestMethod]
        public void TestOnVisible_FromNextStepSkipsToChoiceStep()
        {
            IStepContent<IPublishDialogStep> nextStepContent = null;
            _publishDialogMock.Setup(pd => pd.NavigateToStep(It.IsAny<IStepContent<IPublishDialogStep>>()))
                .Callback<IStepContent<IPublishDialogStep>>(stepContent => nextStepContent = stepContent);
            _objectUnderTest.ActionCommand.Execute(null);

            _objectUnderTest.OnVisible(nextStepContent.ViewModel);

            _publishDialogMock.Verify(pd => pd.PopStep());
        }

        [TestMethod]
        public void TestOnVisible_FromGceStepDeletesChoiceProperty()
        {
            IStepContent<IPublishDialogStep> nextStepContent = null;
            _publishDialogMock.Setup(pd => pd.NavigateToStep(It.IsAny<IStepContent<IPublishDialogStep>>()))
                .Callback<IStepContent<IPublishDialogStep>>(stepContent => nextStepContent = stepContent);
            _objectUnderTest.ActionCommand.Execute(null);

            _objectUnderTest.OnVisible(nextStepContent.ViewModel);

            _propertyServiceMock.Verify(
                ps => ps.DeleteUserProperty(_dteProject, ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName));
        }

        [TestMethod]
        public void TestOnVisible_FromChoiceStepWithPreviousGceChoiceSkipsWarning()
        {
            _propertyServiceMock
                .Setup(
                    ps => ps.GetUserProperty(
                        It.IsAny<Project>(),
                        ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName))
                .Returns(ChoiceType.Gce.ToString());

            _objectUnderTest.OnVisible(new ChoiceStepViewModel(_publishDialogMock.Object));

            _publishDialogMock.Verify(pd => pd.NavigateToStep(It.IsAny<GceStepContent>()));
        }

        [TestMethod]
        public void TestOnVisible_FromChoiceStepWithoutPreviousChoiceShowsWarning()
        {
            _propertyServiceMock
                .Setup(
                    ps => ps.GetUserProperty(
                        It.IsAny<Project>(),
                        ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName))
                .Returns("");

            _objectUnderTest.OnVisible(new ChoiceStepViewModel(_publishDialogMock.Object));

            _publishDialogMock.Verify(pd => pd.NavigateToStep(It.IsAny<GceStepContent>()), Times.Never);
        }

        [TestMethod]
        public void TestOnVisible_FromChoiceStepWithWrongPreviousChoiceShowsWarning()
        {
            _propertyServiceMock
                .Setup(
                    ps => ps.GetUserProperty(
                        It.IsAny<Project>(),
                        ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName))
                .Returns(ChoiceType.Gke.ToString);

            _objectUnderTest.OnVisible(new ChoiceStepViewModel(_publishDialogMock.Object));

            _publishDialogMock.Verify(pd => pd.NavigateToStep(It.IsAny<GceStepContent>()), Times.Never);
        }

        [TestMethod]
        public void TestOnVisible_WithDoNotShowCheckedSkipssWarning()
        {
            GoogleCloudExtensionPackage.Instance.GeneralSettings.DoNotShowAspNetCoreGceWarning = true;
            _propertyServiceMock
                .Setup(
                    ps => ps.GetUserProperty(
                        It.IsAny<Project>(),
                        ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName))
                .Returns("");

            _objectUnderTest.OnVisible(new ChoiceStepViewModel(_publishDialogMock.Object));

            _publishDialogMock.Verify(pd => pd.NavigateToStep(It.IsAny<GceStepContent>()));
        }

        [TestMethod]
        public void TestOnNotVisible_DoesNotThrow() => _objectUnderTest.OnNotVisible();

        [TestMethod]
        public void TestBrowseAspNetCoreIisDocs_OpensBrowser()
        {
            _objectUnderTest.BrowseAspNetCoreIisDocs.Execute(null);

            _browserServiceMock.Verify(b => b.OpenBrowser(CoreGceWarningStepViewModel.AspNetCoreIisDocsLink));
        }
    }
}
