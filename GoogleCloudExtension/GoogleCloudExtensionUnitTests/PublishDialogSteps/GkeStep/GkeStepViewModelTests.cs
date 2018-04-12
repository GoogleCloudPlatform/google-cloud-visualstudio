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
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.PublishDialogSteps.GkeStep;
using GoogleCloudExtensionUnitTests.PublishDialog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.PublishDialogSteps.GkeStep
{
    [TestClass]
    public class GkeStepViewModelTests : PublishDialogStepBaseTestsBase<GkeStepViewModel>
    {
        private static readonly List<Cluster> s_mockedClusters = new List<Cluster>
        {
            new Cluster { Name = "Bcluster" },
            new Cluster { Name = "Acluster" },
            new Cluster { Name = "Ccluster" },
        };

        private Mock<IGkeDataSource> _dataSourceMock;
        private TaskCompletionSource<IList<Cluster>> _clusterListTaskSource;

        private string _expectedDeploymentName;
        private IEnumerable<Cluster> _expectedClusters;
        private Cluster _expectedSelectedCluster;
        private string _expectedReplicas;
        private bool _expectedRefreshClusterListCanExecute;
        private bool _expectedCreateClusterCommandCanExecute;

        protected override int RequieredAPIsForStep => 2;

        protected override GkeStepViewModel CreateStep()
        {
            return GkeStepViewModel.CreateStep(apiManager: _apiManagerMock.Object, pickProjectPrompt: _pickProjectPromptMock.Object, dataSource: _dataSourceMock.Object);
        }

        [TestInitialize]
        public override void BeforeEach()
        {
            _dataSourceMock = new Mock<IGkeDataSource>();
            _dataSourceMock.Setup(ds => ds.GetClusterListAsync()).Returns(() => _clusterListTaskSource.Task);

            base.BeforeEach();
        }

        protected override void InitPositiveValidationMocks()
        {
            base.InitPositiveValidationMocks();
            InitGetClusterListMock(s_mockedClusters);
        }

        protected override void InitNegativeValidationMocks()
        {
            base.InitNegativeValidationMocks();
            InitGetClusterListMock(s_mockedClusters);
        }

        protected override void InitLongRunningValidationMocks()
        {
            base.InitLongRunningValidationMocks();
            InitLongRunningGetClusterMock();
        }

        protected override void InitErrorValidationMocks()
        {
            base.InitErrorValidationMocks();
            InitErrorGetClusterMock();
        }

        private void InitGetClusterListMock(IList<Cluster> result)
        {
            _clusterListTaskSource = new TaskCompletionSource<IList<Cluster>>();
            _clusterListTaskSource.SetResult(result);
        }

        private void InitLongRunningGetClusterMock()
        {
            _clusterListTaskSource = new TaskCompletionSource<IList<Cluster>>();
        }

        private void InitErrorGetClusterMock()
        {
            _clusterListTaskSource = new TaskCompletionSource<IList<Cluster>>();
            _clusterListTaskSource.SetException(new DataSourceException());
        }

        protected override void SetInitialStateExpectedValues()
        {
            base.SetInitialStateExpectedValues();

            _expectedDeploymentName = null;
            _expectedClusters = Enumerable.Empty<Cluster>();
            _expectedSelectedCluster = null;
            _expectedReplicas = "3";
            _expectedRefreshClusterListCanExecute = false;
            _expectedCreateClusterCommandCanExecute = false;
        }

        protected override void SetNoProjectStateExpectedValues()
        {
            base.SetNoProjectStateExpectedValues();

            _expectedDeploymentName = VisualStudioProjectName.ToLower();
            _expectedClusters = Enumerable.Empty<Cluster>();
            _expectedSelectedCluster = null;
            _expectedReplicas = "3";
            _expectedRefreshClusterListCanExecute = false;
            _expectedCreateClusterCommandCanExecute = false;
        }

        protected override void SetValidProjectStateExpectedValues()
        {
            base.SetValidProjectStateExpectedValues();

            _expectedCanPublish = true;

            _expectedDeploymentName = VisualStudioProjectName.ToLower();
            _expectedClusters = s_mockedClusters;
            _expectedSelectedCluster = s_mockedClusters[1];
            _expectedReplicas = "3";
            _expectedRefreshClusterListCanExecute = true;
            _expectedCreateClusterCommandCanExecute = true;
        }

        protected override void SetInvalidProjectStateExpectedValues()
        {
            base.SetInvalidProjectStateExpectedValues();

            _expectedNeedsApiEnabled = true;
            _expectedEnableApiCommandCanExecute = true;
            _expectedShowInputControls = false;

            _expectedDeploymentName = VisualStudioProjectName.ToLower();
            _expectedClusters = Enumerable.Empty<Cluster>();
            _expectedSelectedCluster = null;
            _expectedReplicas = "3";
            _expectedRefreshClusterListCanExecute = false;
            _expectedCreateClusterCommandCanExecute = false;
        }

        protected override void SetErrorInValidationServicesExpectedValues()
        {
            base.SetErrorInValidationServicesExpectedValues();

            _expectedGeneralError = true;
            _expectedShowInputControls = false;

            _expectedDeploymentName = VisualStudioProjectName.ToLower();
            _expectedClusters = Enumerable.Empty<Cluster>();
            _expectedSelectedCluster = null;
            _expectedReplicas = "3";
            _expectedRefreshClusterListCanExecute = false;
            _expectedCreateClusterCommandCanExecute = false;
        }

        protected override void SetLongRunningValidationServicesExpectedValues()
        {
            base.SetLongRunningValidationServicesExpectedValues();

            _expectedLoadingProject = true;
            _expectedShowInputControls = false;

            _expectedDeploymentName = VisualStudioProjectName.ToLower();
            _expectedClusters = Enumerable.Empty<Cluster>();
            _expectedSelectedCluster = null;
            _expectedReplicas = "3";
            _expectedRefreshClusterListCanExecute = false;
            _expectedCreateClusterCommandCanExecute = false;
        }

        protected override void AssertAgainstExpected()
        {
            base.AssertAgainstExpected();

            Assert.AreEqual(_expectedDeploymentName, _objectUnderTest.DeploymentName);
            CollectionAssert.AreEquivalent(_expectedClusters.ToList(), _objectUnderTest.Clusters.ToList());
            Assert.AreEqual(_expectedSelectedCluster, _objectUnderTest.SelectedCluster);
            Assert.AreEqual(_expectedReplicas, _objectUnderTest.Replicas);
            Assert.AreEqual(_expectedRefreshClusterListCanExecute, _objectUnderTest.RefreshClustersListCommand.CanExecuteCommand);
            Assert.AreEqual(_expectedCreateClusterCommandCanExecute, _objectUnderTest.CreateClusterCommand.CanExecuteCommand);
        }
    }
}
