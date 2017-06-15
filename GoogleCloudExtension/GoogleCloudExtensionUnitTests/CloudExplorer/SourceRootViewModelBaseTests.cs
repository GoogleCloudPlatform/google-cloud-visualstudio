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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.CloudExplorer
{
    [TestClass]
    public class SourceRootViewModelBaseTests
    {
        public const string MockProjectId = "mock-project";
        public const string MockExceptionMessage = "MockException";
        private const string MockAccountName = "MockAccount";
        private const string MockRootCaption = "MockRootCaption";
        private const string MockErrorPlaceholderCaption = "MockErrorPlaceholder";
        private const string MockNoItemsPlaceholderCaption = "MockNoItemsPlaceholder";
        private const string MockLoadingPlaceholderCaption = "LoadingPlaceholder";
        private const string ChildCaption = "ChildCaption";

        private Mock<ICloudSourceContext> _contextMock;
        private readonly Project _project = new Project { ProjectId = MockProjectId };
        private readonly UserAccount _userAccount = new UserAccount { AccountName = MockAccountName };
        private readonly TreeNode _childNode = new TreeNode { Caption = ChildCaption };
        private TestableSourceRootviewModelBase _objectUnderTest;

        [TestInitialize]
        public void Initialize()
        {
            _contextMock = new Mock<ICloudSourceContext>();
            _contextMock.Setup(c => c.CurrentProject.ProjectId).Returns(MockProjectId);
            _objectUnderTest = new TestableSourceRootviewModelBase();
        }

        [TestMethod]
        public void TestInitialConditions()
        {
            Assert.AreEqual(0, _objectUnderTest.LoadDataOverrideHitCount);
            Assert.IsNull(_objectUnderTest.Context);
            Assert.IsNull(_objectUnderTest.Icon);
            Assert.IsNull(_objectUnderTest.Caption);
            Assert.AreEqual(0, _objectUnderTest.Children.Count);
            Assert.IsNotNull(_objectUnderTest.LoadingTask);
            Assert.IsTrue(_objectUnderTest.LoadingTask.IsCompleted);
            Assert.IsFalse(_objectUnderTest.IsLoading);
            Assert.IsFalse(_objectUnderTest.IsLoadingState);
            Assert.IsFalse(_objectUnderTest.IsLoadedState);
        }

        [TestMethod]
        public void TestInitialize()
        {
            _objectUnderTest.Initialize(_contextMock.Object);

            Assert.AreEqual(0, _objectUnderTest.LoadDataOverrideHitCount);
            Assert.AreEqual(_contextMock.Object, _objectUnderTest.Context);
            Assert.AreEqual(_objectUnderTest.RootIcon, _objectUnderTest.Icon);
            Assert.AreEqual(_objectUnderTest.RootCaption, _objectUnderTest.Caption);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(_objectUnderTest.LoadingPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestNotExpanded()
        {
            _objectUnderTest.RunOnIsExpandedChanged(false);
            await _objectUnderTest.LoadingTask;

            Assert.AreEqual(0, _objectUnderTest.LoadDataOverrideHitCount);
            Assert.IsFalse(_objectUnderTest.IsLoading);
            Assert.IsFalse(_objectUnderTest.IsLoadingState);
            Assert.IsFalse(_objectUnderTest.IsLoadedState);
            Assert.AreEqual(0, _objectUnderTest.Children.Count);
        }

        [TestMethod]
        public void TestLoadingNoCredentials()
        {
            CredentialsStore.Default.UpdateCurrentProject(_project);
            CredentialsStore.Default.CurrentAccount = null;
            var originalLoadingTask = _objectUnderTest.LoadingTask;

            _objectUnderTest.RunOnIsExpandedChanged(true);

            Assert.AreEqual(0, _objectUnderTest.LoadDataOverrideHitCount);
            Assert.IsTrue(_objectUnderTest.LoadingTask.IsCompleted);
            Assert.AreNotSame(originalLoadingTask, _objectUnderTest.LoadingTask);
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
            CredentialsStore.Default.CurrentAccount = _userAccount;

            _objectUnderTest.RunOnIsExpandedChanged(true);
            await _objectUnderTest.LoadingTask;

            Assert.AreEqual(0, _objectUnderTest.LoadDataOverrideHitCount);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            var child = _objectUnderTest.Children[0] as TreeLeaf;
            Assert.IsNotNull(child);
            Assert.AreEqual(Resources.CloudExplorerNoProjectSelectedMessage, child.Caption);
            Assert.IsFalse(child.IsLoading);
            Assert.IsFalse(child.IsWarning);
            Assert.IsTrue(child.IsError);
        }

        [TestMethod]
        public void TestLoadingState()
        {
            CredentialsStore.Default.UpdateCurrentProject(_project);
            CredentialsStore.Default.CurrentAccount = _userAccount;

            _objectUnderTest.Initialize(_contextMock.Object);
            _objectUnderTest.RunOnIsExpandedChanged(true);

            Assert.AreEqual(1, _objectUnderTest.LoadDataOverrideHitCount);
            Assert.IsFalse(_objectUnderTest.LoadingTask.IsCompleted);
            Assert.IsTrue(_objectUnderTest.IsLoadingState);
            Assert.IsFalse(_objectUnderTest.IsLoadedState);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(_objectUnderTest.LoadingPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public void TestLoadingAgain()
        {
            CredentialsStore.Default.UpdateCurrentProject(_project);
            CredentialsStore.Default.CurrentAccount = _userAccount;

            _objectUnderTest.Initialize(_contextMock.Object);
            _objectUnderTest.RunOnIsExpandedChanged(true);
            _objectUnderTest.RunOnIsExpandedChanged(true);

            Assert.AreEqual(1, _objectUnderTest.LoadDataOverrideHitCount);
            Assert.IsFalse(_objectUnderTest.LoadingTask.IsCompleted);
            Assert.IsTrue(_objectUnderTest.IsLoadingState);
            Assert.IsFalse(_objectUnderTest.IsLoadedState);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(_objectUnderTest.LoadingPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestLoadingError()
        {
            CredentialsStore.Default.UpdateCurrentProject(_project);
            CredentialsStore.Default.CurrentAccount = _userAccount;

            _objectUnderTest.RunOnIsExpandedChanged(true);
            _objectUnderTest.LoadSource.SetException(new CloudExplorerSourceException(MockExceptionMessage));
            await _objectUnderTest.LoadingTask;

            Assert.AreEqual(1, _objectUnderTest.LoadDataOverrideHitCount);
            Assert.IsFalse(_objectUnderTest.IsLoadingState);
            Assert.IsTrue(_objectUnderTest.IsLoadedState);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(_objectUnderTest.ErrorPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestLoadingNoItems()
        {
            CredentialsStore.Default.UpdateCurrentProject(_project);
            CredentialsStore.Default.CurrentAccount = _userAccount;

            _objectUnderTest.RunOnIsExpandedChanged(true);
            _objectUnderTest.LoadSource.SetResult(new List<TreeNode>());
            await _objectUnderTest.LoadingTask;

            Assert.AreEqual(1, _objectUnderTest.LoadDataOverrideHitCount);
            Assert.IsFalse(_objectUnderTest.IsLoadingState);
            Assert.IsTrue(_objectUnderTest.IsLoadedState);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(_objectUnderTest.NoItemsPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestLoadingSuccess()
        {
            CredentialsStore.Default.UpdateCurrentProject(_project);
            CredentialsStore.Default.CurrentAccount = _userAccount;

            _objectUnderTest.RunOnIsExpandedChanged(true);
            _objectUnderTest.LoadSource.SetResult(new List<TreeNode> { _childNode });
            await _objectUnderTest.LoadingTask;

            Assert.AreEqual(1, _objectUnderTest.LoadDataOverrideHitCount);
            Assert.IsFalse(_objectUnderTest.IsLoadingState);
            Assert.IsTrue(_objectUnderTest.IsLoadedState);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(_childNode, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestRefreshingUnloaded()
        {
            _objectUnderTest.Refresh();
            await _objectUnderTest.LoadingTask;

            Assert.AreEqual(0, _objectUnderTest.LoadDataOverrideHitCount);
            Assert.IsFalse(_objectUnderTest.IsLoadingState);
            Assert.IsFalse(_objectUnderTest.IsLoadedState);
            Assert.AreEqual(0, _objectUnderTest.Children.Count);
        }

        [TestMethod]
        public void TestRefreshOnLoading()
        {
            CredentialsStore.Default.UpdateCurrentProject(_project);
            CredentialsStore.Default.CurrentAccount = _userAccount;

            _objectUnderTest.Initialize(_contextMock.Object);
            _objectUnderTest.RunOnIsExpandedChanged(true);
            var expandLoadTask = _objectUnderTest.LoadingTask;
            _objectUnderTest.Refresh();

            Assert.AreEqual(1, _objectUnderTest.LoadDataOverrideHitCount);
            Assert.AreEqual(expandLoadTask, _objectUnderTest.LoadingTask);
            Assert.IsTrue(_objectUnderTest.IsLoadingState);
            Assert.IsFalse(_objectUnderTest.IsLoadedState);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(_objectUnderTest.LoadingPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestRefreshLoading()
        {
            CredentialsStore.Default.UpdateCurrentProject(_project);
            CredentialsStore.Default.CurrentAccount = _userAccount;

            _objectUnderTest.RunOnIsExpandedChanged(true);
            _objectUnderTest.LoadSource.SetResult(new List<TreeNode>());
            await _objectUnderTest.LoadingTask;
            _objectUnderTest.Refresh();

            Assert.AreEqual(2, _objectUnderTest.LoadDataOverrideHitCount);
            Assert.IsTrue(_objectUnderTest.IsLoadingState);
            Assert.IsFalse(_objectUnderTest.IsLoadedState);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(_objectUnderTest.LoadingPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestRefreshLoadError()
        {
            CredentialsStore.Default.UpdateCurrentProject(_project);
            CredentialsStore.Default.CurrentAccount = _userAccount;

            _objectUnderTest.RunOnIsExpandedChanged(true);
            _objectUnderTest.LoadSource.SetResult(new List<TreeNode>());
            await _objectUnderTest.LoadingTask;
            _objectUnderTest.Refresh();
            _objectUnderTest.LoadSource.SetException(new CloudExplorerSourceException(MockExceptionMessage));
            await _objectUnderTest.LoadingTask;

            Assert.AreEqual(2, _objectUnderTest.LoadDataOverrideHitCount);
            Assert.IsFalse(_objectUnderTest.IsLoadingState);
            Assert.IsTrue(_objectUnderTest.IsLoadedState);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(_objectUnderTest.ErrorPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestRefreshLoadNoItems()
        {
            CredentialsStore.Default.UpdateCurrentProject(_project);
            CredentialsStore.Default.CurrentAccount = _userAccount;

            _objectUnderTest.RunOnIsExpandedChanged(true);
            _objectUnderTest.LoadSource.SetException(new CloudExplorerSourceException(MockExceptionMessage));
            await _objectUnderTest.LoadingTask;
            _objectUnderTest.Refresh();
            _objectUnderTest.LoadSource.SetResult(new List<TreeNode>());
            await _objectUnderTest.LoadingTask;

            Assert.AreEqual(2, _objectUnderTest.LoadDataOverrideHitCount);
            Assert.IsFalse(_objectUnderTest.IsLoadingState);
            Assert.IsTrue(_objectUnderTest.IsLoadedState);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(_objectUnderTest.NoItemsPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestRefreshLoadSuccess()
        {
            CredentialsStore.Default.UpdateCurrentProject(_project);
            CredentialsStore.Default.CurrentAccount = _userAccount;

            _objectUnderTest.RunOnIsExpandedChanged(true);
            _objectUnderTest.LoadSource.SetException(new CloudExplorerSourceException(MockExceptionMessage));
            await _objectUnderTest.LoadingTask;
            _objectUnderTest.Refresh();
            _objectUnderTest.LoadSource.SetResult(new List<TreeNode> { _childNode });
            await _objectUnderTest.LoadingTask;

            Assert.AreEqual(2, _objectUnderTest.LoadDataOverrideHitCount);
            Assert.IsFalse(_objectUnderTest.IsLoadingState);
            Assert.IsTrue(_objectUnderTest.IsLoadedState);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(_childNode, _objectUnderTest.Children[0]);
        }

        private class TestableSourceRootviewModelBase : SourceRootViewModelBase
        {

            public TaskCompletionSource<IList<TreeNode>> LoadSource { get; private set; } =
                new TaskCompletionSource<IList<TreeNode>>();

            public override string RootCaption => MockRootCaption;

            public override TreeLeaf ErrorPlaceholder { get; } = new TreeLeaf
            {
                Caption = MockErrorPlaceholderCaption,
                IsError = true
            };

            public override TreeLeaf NoItemsPlaceholder { get; } = new TreeLeaf
            {
                Caption = MockNoItemsPlaceholderCaption,
                IsWarning = true
            };

            public override TreeLeaf LoadingPlaceholder { get; } = new TreeLeaf
            {
                Caption = MockLoadingPlaceholderCaption,
                IsLoading = true
            };

            protected override async Task LoadDataOverride()
            {
                LoadDataOverrideHitCount++;
                IList<TreeNode> childNodes;
                try
                {
                    childNodes = await LoadSource.Task;
                }
                finally
                {
                    LoadSource = new TaskCompletionSource<IList<TreeNode>>();
                }
                Children.Clear();
                foreach (TreeNode child in childNodes)
                {
                    Children.Add(child);
                }
            }

            public int LoadDataOverrideHitCount { get; private set; } = 0;

            internal void RunOnIsExpandedChanged(bool newValue)
            {
                OnIsExpandedChanged(newValue);
            }
        }
    }
}