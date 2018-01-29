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
using GoogleCloudExtension;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.CloudExplorer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.CloudExplorer
{
    [TestClass]
    public class SourceRootViewModelBaseTests
    {
        public const string MockProjectId = "parent.com:mock-project";
        public const string MockExceptionMessage = "MockException";
        private const string MockAccountName = "MockAccount";
        private const string MockRootCaption = "MockRootCaption";
        private const string MockErrorPlaceholderCaption = "MockErrorPlaceholder";
        private const string MockNoItemsPlaceholderCaption = "MockNoItemsPlaceholder";
        private const string MockLoadingPlaceholderCaption = "LoadingPlaceholder";
        private const string ChildCaption = "ChildCaption";

        private static readonly TreeLeaf s_loadingPlaceholder = new TreeLeaf
        {
            Caption = MockLoadingPlaceholderCaption,
            IsLoading = true
        };

        private static readonly TreeLeaf s_noItemsPlaceholder = new TreeLeaf
        {
            Caption = MockNoItemsPlaceholderCaption,
            IsWarning = true
        };

        private static readonly TreeLeaf s_errorPlaceholder = new TreeLeaf
        {
            Caption = MockErrorPlaceholderCaption,
            IsError = true
        };
        private static readonly Project s_project = new Project { ProjectId = MockProjectId };
        private static readonly UserAccount s_userAccount = new UserAccount { AccountName = MockAccountName };
        private static readonly TreeNode s_childNode = new TreeNode { Caption = ChildCaption };

        private TaskCompletionSource<IList<TreeNode>> _loadDataSource;
        private ICloudSourceContext _mockedContext;
        private SourceRootViewModelBase _objectUnderTest;
        private Mock<SourceRootViewModelBase> _objectUnderTestMock;

        [TestInitialize]
        public void Initialize()
        {
            CredentialsStore.Default.UpdateCurrentAccount(s_userAccount);
            CredentialsStore.Default.UpdateCurrentProject(s_project);

            _loadDataSource = new TaskCompletionSource<IList<TreeNode>>();

            _mockedContext = Mock.Of<ICloudSourceContext>(c => c.CurrentProject.ProjectId == MockProjectId);
            _objectUnderTest = Mock.Of<SourceRootViewModelBase>(
                o => o.RootCaption == MockRootCaption &&
                o.ErrorPlaceholder == s_errorPlaceholder &&
                o.LoadingPlaceholder == s_loadingPlaceholder &&
                o.NoItemsPlaceholder == s_noItemsPlaceholder);
            _objectUnderTestMock = Mock.Get(_objectUnderTest);
            _objectUnderTestMock.CallBase = true;
            _objectUnderTestMock.Protected().Setup<Task>("LoadDataOverride").Returns(
                async () =>
                {
                    IList<TreeNode> childNodes;
                    try
                    {
                        childNodes = await _loadDataSource.Task;
                    }
                    finally
                    {
                        _loadDataSource = new TaskCompletionSource<IList<TreeNode>>();
                    }
                    _objectUnderTest.Children.Clear();
                    foreach (TreeNode child in childNodes)
                    {
                        _objectUnderTest.Children.Add(child);
                    }
                });
            _objectUnderTest = _objectUnderTestMock.Object;
        }

        [TestMethod]
        public void TestInitialConditions()
        {
            _objectUnderTestMock.Protected().Verify("LoadDataOverride", Times.Never());
            Assert.IsNull(_objectUnderTest.Context);
            Assert.IsNull(_objectUnderTest.Icon);
            Assert.IsNull(_objectUnderTest.Caption);
            Assert.AreEqual(0, _objectUnderTest.Children.Count);
        }

        [TestMethod]
        public void TestInitialize()
        {
            _objectUnderTest.Initialize(_mockedContext);

            _objectUnderTestMock.Protected().Verify("LoadDataOverride", Times.Never());
            Assert.AreEqual(_mockedContext, _objectUnderTest.Context);
            Assert.AreEqual(_objectUnderTest.RootIcon, _objectUnderTest.Icon);
            Assert.AreEqual(_objectUnderTest.RootCaption, _objectUnderTest.Caption);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_loadingPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestNotExpanded()
        {
            _objectUnderTest.IsExpanded = false;
            await _objectUnderTest.LoadingTask;

            _objectUnderTestMock.Protected().Verify("LoadDataOverride", Times.Never());
            Assert.AreEqual(0, _objectUnderTest.Children.Count);
        }

        [TestMethod]
        public async Task TestLoadingNoCredentials()
        {
            CredentialsStore.Default.UpdateCurrentAccount(null);

            _objectUnderTest.IsExpanded = true;
            await _objectUnderTest.LoadingTask;

            _objectUnderTestMock.Protected().Verify("LoadDataOverride", Times.Never());
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            var child = _objectUnderTest.Children[0] as TreeLeaf;
            Assert.IsNotNull(child);
            Assert.AreEqual(Resources.CloudExplorerNoLoggedInMessage, child.Caption);
            Assert.IsFalse(child.IsLoading);
            Assert.IsFalse(child.IsWarning);
            Assert.IsTrue(child.IsError);
        }

        [TestMethod]
        public async Task TestLoadingNoProject()
        {
            CredentialsStore.Default.UpdateCurrentProject(null);

            _objectUnderTest.IsExpanded = true;
            await _objectUnderTest.LoadingTask;

            _objectUnderTestMock.Protected().Verify("LoadDataOverride", Times.Never());
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            var child = _objectUnderTest.Children[0] as TreeLeaf;
            Assert.IsNotNull(child);
            Assert.AreEqual(Resources.CloudExplorerNoProjectSelectedMessage, child.Caption);
            Assert.IsFalse(child.IsLoading);
            Assert.IsFalse(child.IsWarning);
            Assert.IsTrue(child.IsError);
        }

        [TestMethod]
        public void TestLoading()
        {
            _objectUnderTest.Initialize(_mockedContext);
            _objectUnderTest.IsExpanded = true;

            _objectUnderTestMock.Protected().Verify("LoadDataOverride", Times.Once());
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_loadingPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestLoadingError()
        {
            _objectUnderTest.IsExpanded = true;
            _loadDataSource.SetException(new CloudExplorerSourceException(MockExceptionMessage));
            await _objectUnderTest.LoadingTask;

            _objectUnderTestMock.Protected().Verify("LoadDataOverride", Times.Once());
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_errorPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestLoadingNoItems()
        {
            _objectUnderTest.IsExpanded = true;
            _loadDataSource.SetResult(new List<TreeNode>());
            await _objectUnderTest.LoadingTask;

            _objectUnderTestMock.Protected().Verify("LoadDataOverride", Times.Once());
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_noItemsPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestLoadingSuccess()
        {
            _objectUnderTest.IsExpanded = true;
            _loadDataSource.SetResult(new List<TreeNode> { s_childNode });
            await _objectUnderTest.LoadingTask;

            _objectUnderTestMock.Protected().Verify("LoadDataOverride", Times.Once());
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_childNode, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestDoubleLoading()
        {
            _objectUnderTest.Initialize(_mockedContext);
            _objectUnderTest.IsExpanded = true;
            _objectUnderTest.IsExpanded = false;
            _objectUnderTest.IsExpanded = true;
            _loadDataSource.SetResult(new List<TreeNode>());
            await _objectUnderTest.LoadingTask;
            _loadDataSource.SetException(new CloudExplorerSourceException(MockExceptionMessage));
            await _objectUnderTest.LoadingTask;

            _objectUnderTestMock.Protected().Verify("LoadDataOverride", Times.Once());
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_noItemsPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestReloading()
        {
            _objectUnderTest.Initialize(_mockedContext);
            _objectUnderTest.IsExpanded = true;
            _loadDataSource.SetResult(new List<TreeNode>());
            await _objectUnderTest.LoadingTask;
            _objectUnderTest.IsExpanded = false;
            _objectUnderTest.IsExpanded = true;
            _loadDataSource.SetException(new CloudExplorerSourceException(MockExceptionMessage));
            await _objectUnderTest.LoadingTask;

            _objectUnderTestMock.Protected().Verify("LoadDataOverride", Times.Once());
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_noItemsPlaceholder.Caption, _objectUnderTest.Children[0].Caption);
        }

        [TestMethod]
        public async Task TestRefreshingUninitialized()
        {
            _objectUnderTest.Refresh();
            await _objectUnderTest.LoadingTask;

            _objectUnderTestMock.Protected().Verify("LoadDataOverride", Times.Never());
            Assert.AreEqual(0, _objectUnderTest.Children.Count);
        }

        [TestMethod]
        public async Task TestRefreshingUnloaded()
        {
            _objectUnderTest.Initialize(_mockedContext);
            _objectUnderTest.Refresh();
            await _objectUnderTest.LoadingTask;

            _objectUnderTestMock.Protected().Verify("LoadDataOverride", Times.Never());
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_loadingPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestRefreshOnLoading()
        {
            _objectUnderTest.Initialize(_mockedContext);
            _objectUnderTest.IsExpanded = true;
            _objectUnderTest.Refresh();
            _loadDataSource.SetResult(new List<TreeNode>());
            await _objectUnderTest.LoadingTask;
            _loadDataSource.SetException(new CloudExplorerSourceException(MockExceptionMessage));
            await _objectUnderTest.LoadingTask;

            _objectUnderTestMock.Protected().Verify("LoadDataOverride", Times.Once());
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_noItemsPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestRefreshLoading()
        {
            _objectUnderTest.IsExpanded = true;
            _loadDataSource.SetResult(new List<TreeNode>());
            await _objectUnderTest.LoadingTask;
            _objectUnderTest.Refresh();

            _objectUnderTestMock.Protected().Verify("LoadDataOverride", Times.Exactly(2));
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_loadingPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestRefreshLoadError()
        {
            _objectUnderTest.IsExpanded = true;
            _loadDataSource.SetResult(new List<TreeNode>());
            await _objectUnderTest.LoadingTask;
            _objectUnderTest.Refresh();
            _loadDataSource.SetException(new CloudExplorerSourceException(MockExceptionMessage));
            await _objectUnderTest.LoadingTask;

            _objectUnderTestMock.Protected().Verify("LoadDataOverride", Times.Exactly(2));
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_errorPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestRefreshLoadNoItems()
        {
            _objectUnderTest.IsExpanded = true;
            _loadDataSource.SetException(new CloudExplorerSourceException(MockExceptionMessage));
            await _objectUnderTest.LoadingTask;
            _objectUnderTest.Refresh();
            _loadDataSource.SetResult(new List<TreeNode> { s_childNode });
            await _objectUnderTest.LoadingTask;

            _objectUnderTestMock.Protected().Verify("LoadDataOverride", Times.Exactly(2));
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_childNode, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestRefreshLoadSuccess()
        {
            _objectUnderTest.IsExpanded = true;
            _loadDataSource.SetException(new CloudExplorerSourceException(MockExceptionMessage));
            await _objectUnderTest.LoadingTask;
            _objectUnderTest.Refresh();
            _loadDataSource.SetResult(new List<TreeNode> { s_childNode });
            await _objectUnderTest.LoadingTask;

            _objectUnderTestMock.Protected().Verify("LoadDataOverride", Times.Exactly(2));
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_childNode, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestDoubleRefresh()
        {
            _objectUnderTest.IsExpanded = true;
            _loadDataSource.SetResult(new List<TreeNode>());
            await _objectUnderTest.LoadingTask;
            _objectUnderTest.Refresh();
            _objectUnderTest.Refresh();
            _loadDataSource.SetResult(new List<TreeNode> { s_childNode });
            await _objectUnderTest.LoadingTask;
            _loadDataSource.SetException(new CloudExplorerSourceException(MockExceptionMessage));
            await _objectUnderTest.LoadingTask;

            _objectUnderTestMock.Protected().Verify("LoadDataOverride", Times.Exactly(2));
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_childNode, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestRefreshAgain()
        {
            _objectUnderTest.IsExpanded = true;
            _loadDataSource.SetException(new CloudExplorerSourceException(MockExceptionMessage));
            await _objectUnderTest.LoadingTask;
            _objectUnderTest.Refresh();
            _loadDataSource.SetResult(new List<TreeNode>());
            await _objectUnderTest.LoadingTask;
            _objectUnderTest.Refresh();
            _loadDataSource.SetResult(new List<TreeNode> { s_childNode });
            await _objectUnderTest.LoadingTask;

            _objectUnderTestMock.Protected().Verify("LoadDataOverride", Times.Exactly(3));
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_childNode, _objectUnderTest.Children[0]);
        }
    }
}