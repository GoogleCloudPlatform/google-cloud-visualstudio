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

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialogSteps.GceStep;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.PublishDialogSteps.GceStep
{
    /// <summary>
    /// Unit tests for <seealso cref="GceStepViewModel"/>
    /// </summary>
    [TestClass]
    public class GceStepViewModelTests
    {
        private const string WindowsLicenseUrl = "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/licenses/windows-server-2016-dc";

        private static readonly AttachedDisk s_windowsDisk = new AttachedDisk
        {
            Licenses = new string[] { WindowsLicenseUrl },
            Boot = true
        };

        private static readonly IList<Instance> s_mockedInstances = new List<Instance>
        {
            new Instance { Name="Ainstance", Disks = new AttachedDisk[] { s_windowsDisk }, Status = "RUNNING" }
        };

        private GceStepViewModel _objectUnderTest;
        private Mock<IApiManager> _mockedApiManager;
        private Mock<IGceDataSource> _mockedDataSource;
        private Mock<IPublishDialog> _mockedPublishDialog;
        private TaskCompletionSource<bool> _areServicesEnabledTaskSource;
        private TaskCompletionSource<IList<Instance>> _getInstanceListTaskSource;

        [TestInitialize]
        public void InitializeTest()
        {
            _mockedApiManager = new Mock<IApiManager>();
            _mockedDataSource = new Mock<IGceDataSource>();
            _mockedPublishDialog = new Mock<IPublishDialog>();

            _areServicesEnabledTaskSource = new TaskCompletionSource<bool>();
            _getInstanceListTaskSource = new TaskCompletionSource<IList<Instance>>();

            _mockedApiManager.Setup(x => x.AreServicesEnabledAsync(It.IsAny<IList<string>>())).Returns(_areServicesEnabledTaskSource.Task);
            _mockedDataSource.Setup(x => x.GetInstanceListAsync()).Returns(_getInstanceListTaskSource.Task);

            _objectUnderTest = GceStepViewModel.CreateStep(_mockedDataSource.Object, _mockedApiManager.Object);
        }

        [TestMethod]
        public void TestInitialState()
        {
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
        }

        [TestMethod]
        public void TestStateAfterOnPushedToDialog()
        {
            _objectUnderTest.OnPushedToDialog(_mockedPublishDialog.Object);

            Assert.IsNotNull(_objectUnderTest.LoadingProjectTask);
            Assert.IsTrue(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
        }

        [TestMethod]
        public async Task TestPositiveProjectValidation()
        {
            _objectUnderTest.OnPushedToDialog(_mockedPublishDialog.Object);
            _areServicesEnabledTaskSource.SetResult(true);
            _getInstanceListTaskSource.SetResult(s_mockedInstances);

            await _objectUnderTest.LoadingProjectTask;

            Assert.IsTrue(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsNotNull(_objectUnderTest.Instances);
            Assert.AreEqual(s_mockedInstances.Count, _objectUnderTest.Instances.Count());
        }

        [TestMethod]
        public async Task TestNeedsApiValidation()
        {
            _objectUnderTest.OnPushedToDialog(_mockedPublishDialog.Object);
            _areServicesEnabledTaskSource.SetResult(false);

            await _objectUnderTest.LoadingProjectTask;

            Assert.IsTrue(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.GeneralError);
        }

        [TestMethod]
        public async Task TestErrorCheckingServices()
        {
            _objectUnderTest.OnPushedToDialog(_mockedPublishDialog.Object);
            _areServicesEnabledTaskSource.SetException(new DataSourceException());

            await _objectUnderTest.LoadingProjectTask;

            Assert.IsTrue(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
        }

        [TestMethod]
        public async Task TestErrorLoadingInstances()
        {
            _objectUnderTest.OnPushedToDialog(_mockedPublishDialog.Object);
            _areServicesEnabledTaskSource.SetResult(true);
            _getInstanceListTaskSource.SetException(new DataSourceException());

            await _objectUnderTest.LoadingProjectTask;

            Assert.IsTrue(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
        }
    }
}
