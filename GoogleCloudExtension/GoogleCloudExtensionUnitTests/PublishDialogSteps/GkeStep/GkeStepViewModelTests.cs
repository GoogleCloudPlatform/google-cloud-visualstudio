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

using Google.Apis.Container.v1.Data;
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialogSteps.GkeStep;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.PublishDialogSteps.GkeStep
{
    /// <summary>
    /// Unit tests for <seealso cref="GkeStepViewModel"/>.
    /// </summary>
    [TestClass]
    public class GkeStepViewModelTests
    {
        private const string ProjectId = "ProjectId";

        private static readonly List<Cluster> s_mockedClusters = new List<Cluster>
        {
            new Cluster { Name = "Bcluster" },
            new Cluster { Name = "Acluster" },
            new Cluster { Name = "Ccluster" },
        };

        private GkeStepViewModel _objectUnderTest;
        private Mock<IApiManager> _mockedApiManager;
        private Mock<IGkeDataSource> _mockedDataSource;
        private Mock<IPublishDialog> _mockedPublishDialog;
        private Mock<IParsedProject> _mockedProject;
        private TaskCompletionSource<bool> _areServicesEnabledTaskSource;
        private TaskCompletionSource<IList<Cluster>> _clusterListTaskSource;

        [TestInitialize]
        public void Initialize()
        {
            _mockedApiManager = new Mock<IApiManager>();
            _mockedDataSource = new Mock<IGkeDataSource>();
            _mockedPublishDialog = new Mock<IPublishDialog>();
            _mockedProject = new Mock<IParsedProject>();

            _areServicesEnabledTaskSource = new TaskCompletionSource<bool>();
            _clusterListTaskSource = new TaskCompletionSource<IList<Cluster>>();

            _mockedApiManager.Setup(x => x.AreServicesEnabledAsync(It.IsAny<IList<string>>())).Returns(() => _areServicesEnabledTaskSource.Task);
            _mockedDataSource.Setup(x => x.GetClusterListAsync()).Returns(() => _clusterListTaskSource.Task);

            _mockedProject.Setup(x => x.Name).Returns(ProjectId);

            _mockedPublishDialog.Setup(x => x.TrackTask(It.IsAny<Task>()));
            _mockedPublishDialog.Setup(x => x.Project).Returns(_mockedProject.Object);

            _objectUnderTest = GkeStepViewModel.CreateStep(_mockedDataSource.Object, _mockedApiManager.Object);
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
            _clusterListTaskSource.SetResult(s_mockedClusters);

            await _objectUnderTest.LoadingProjectTask;

            Assert.IsTrue(_objectUnderTest.CanPublish);
            Assert.IsTrue(_objectUnderTest.RefreshClustersListCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.CreateClusterCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsNotNull(_objectUnderTest.Clusters);
            Assert.AreEqual(ProjectId.ToLower(), _objectUnderTest.DeploymentName);
            Assert.AreEqual(s_mockedClusters.Count, _objectUnderTest.Clusters.Count());
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
        public async Task TestErrorDuringApiValidation()
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
        public async Task TestErrorDuringClustersLoad()
        {
            _objectUnderTest.OnPushedToDialog(_mockedPublishDialog.Object);
            _areServicesEnabledTaskSource.SetResult(true);
            _clusterListTaskSource.SetException(new DataSourceException());

            await _objectUnderTest.LoadingProjectTask;

            Assert.IsTrue(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
        }
    }
}
