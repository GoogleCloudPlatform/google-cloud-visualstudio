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

using GoogleCloudExtension;
using GoogleCloudExtension.CloudExplorer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.CloudExplorer
{
    [TestClass]
    public class SourceRootViewModelBaseTests : ExtensionTestBase
    {
        private const string MockExceptionMessage = "MockException";
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
        private static readonly TreeNode s_childNode = new TreeNode { Caption = ChildCaption };

        private ICloudSourceContext _mockedContext;
        private TestableSourceRootViewModelBase _objectUnderTest;

        protected override void BeforeEach()
        {

            _mockedContext = Mock.Of<ICloudSourceContext>();
            _objectUnderTest = new TestableSourceRootViewModelBase();
        }

        private class TestableSourceRootViewModelBase : SourceRootViewModelBase
        {
            public TaskCompletionSource<IList<TreeNode>> LoadDataSource = new TaskCompletionSource<IList<TreeNode>>();

            /// <summary>
            /// Returns the caption to use for the root node for this data source.
            /// </summary>
            public override string RootCaption { get; } = MockRootCaption;

            /// <summary>
            /// Returns the tree node to use when there's an error loading data.
            /// </summary>
            public override TreeLeaf ErrorPlaceholder { get; } = s_errorPlaceholder;

            /// <summary>
            /// Returns the tree node to use when there's no data returned by this data source.
            /// </summary>
            public override TreeLeaf NoItemsPlaceholder { get; } = s_noItemsPlaceholder;

            /// <summary>
            /// Returns the tree node to use while loading data.
            /// </summary>
            public override TreeLeaf LoadingPlaceholder { get; } = s_loadingPlaceholder;

            /// <summary>
            /// Returns the tree node to use when we detect that the necessary APIs are not enabled.
            /// </summary>
            public override TreeLeaf ApiNotEnabledPlaceholder { get; }

            /// <summary>
            /// Returns the names of the required APIs for the source.
            /// </summary>
            public override IList<string> RequiredApis { get; }

            /// <summary>
            /// Override this function to load and display the data in the control.
            /// </summary>
            protected override async Task LoadDataOverrideAsync()
            {
                LoadDataOverrideCallCount++;
                IList<TreeNode> children;
                try
                {
                    children = await LoadDataSource.Task;
                }
                finally
                {
                    LoadDataSource = new TaskCompletionSource<IList<TreeNode>>();
                }

                Children.Clear();
                foreach (TreeNode child in children)
                {
                    Children.Add(child);
                }
            }

            public int LoadDataOverrideCallCount { get; private set; }
        }

        [TestMethod]
        public void TestInitialConditions()
        {
            Assert.AreEqual(0, _objectUnderTest.LoadDataOverrideCallCount);
            Assert.IsNull(_objectUnderTest.Context);
            Assert.IsNull(_objectUnderTest.Icon);
            Assert.IsNull(_objectUnderTest.Caption);
            Assert.AreEqual(0, _objectUnderTest.Children.Count);
        }

        [TestMethod]
        public void TestInitialize()
        {
            _objectUnderTest.Initialize(_mockedContext);

            Assert.AreEqual(0, _objectUnderTest.LoadDataOverrideCallCount);
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

            Assert.AreEqual(0, _objectUnderTest.LoadDataOverrideCallCount);
            Assert.AreEqual(0, _objectUnderTest.Children.Count);
        }

        [TestMethod]
        public async Task TestLoadingNoCredentials()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentAccount).Returns(() => null);

            _objectUnderTest.IsExpanded = true;
            await _objectUnderTest.LoadingTask;

            Assert.AreEqual(0, _objectUnderTest.LoadDataOverrideCallCount);
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
            CredentialStoreMock.SetupGet(cs => cs.CurrentProjectId).Returns(() => null);

            _objectUnderTest.IsExpanded = true;
            await _objectUnderTest.LoadingTask;

            Assert.AreEqual(0, _objectUnderTest.LoadDataOverrideCallCount);
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

            Assert.AreEqual(1, _objectUnderTest.LoadDataOverrideCallCount);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_loadingPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestLoadingError()
        {
            _objectUnderTest.IsExpanded = true;
            _objectUnderTest.LoadDataSource.SetException(new CloudExplorerSourceException(MockExceptionMessage));
            await _objectUnderTest.LoadingTask;

            Assert.AreEqual(1, _objectUnderTest.LoadDataOverrideCallCount);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_errorPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestLoadingNoItems()
        {
            _objectUnderTest.IsExpanded = true;
            _objectUnderTest.LoadDataSource.SetResult(new List<TreeNode>());
            await _objectUnderTest.LoadingTask;

            Assert.AreEqual(1, _objectUnderTest.LoadDataOverrideCallCount);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_noItemsPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestLoadingSuccess()
        {
            _objectUnderTest.IsExpanded = true;
            _objectUnderTest.LoadDataSource.SetResult(new List<TreeNode> { s_childNode });
            await _objectUnderTest.LoadingTask;

            Assert.AreEqual(1, _objectUnderTest.LoadDataOverrideCallCount);
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
            _objectUnderTest.LoadDataSource.SetResult(new List<TreeNode>());
            await _objectUnderTest.LoadingTask;
            _objectUnderTest.LoadDataSource.SetException(new CloudExplorerSourceException(MockExceptionMessage));
            await _objectUnderTest.LoadingTask;

            Assert.AreEqual(1, _objectUnderTest.LoadDataOverrideCallCount);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_noItemsPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestReloading()
        {
            _objectUnderTest.Initialize(_mockedContext);
            _objectUnderTest.IsExpanded = true;
            _objectUnderTest.LoadDataSource.SetResult(new List<TreeNode>());
            await _objectUnderTest.LoadingTask;
            _objectUnderTest.IsExpanded = false;
            _objectUnderTest.IsExpanded = true;
            _objectUnderTest.LoadDataSource.SetException(new CloudExplorerSourceException(MockExceptionMessage));
            await _objectUnderTest.LoadingTask;

            Assert.AreEqual(1, _objectUnderTest.LoadDataOverrideCallCount);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_noItemsPlaceholder.Caption, _objectUnderTest.Children[0].Caption);
        }

        [TestMethod]
        public async Task TestRefreshingUninitialized()
        {
            _objectUnderTest.Refresh();
            await _objectUnderTest.LoadingTask;

            Assert.AreEqual(0, _objectUnderTest.LoadDataOverrideCallCount);
            Assert.AreEqual(0, _objectUnderTest.Children.Count);
        }

        [TestMethod]
        public async Task TestRefreshingUnloaded()
        {
            _objectUnderTest.Initialize(_mockedContext);
            _objectUnderTest.Refresh();
            await _objectUnderTest.LoadingTask;

            Assert.AreEqual(0, _objectUnderTest.LoadDataOverrideCallCount);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_loadingPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestRefreshOnLoading()
        {
            _objectUnderTest.Initialize(_mockedContext);
            _objectUnderTest.IsExpanded = true;
            _objectUnderTest.Refresh();
            _objectUnderTest.LoadDataSource.SetResult(new List<TreeNode>());
            await _objectUnderTest.LoadingTask;
            _objectUnderTest.LoadDataSource.SetException(new CloudExplorerSourceException(MockExceptionMessage));
            await _objectUnderTest.LoadingTask;

            Assert.AreEqual(1, _objectUnderTest.LoadDataOverrideCallCount);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_noItemsPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestRefreshLoading()
        {
            _objectUnderTest.IsExpanded = true;
            _objectUnderTest.LoadDataSource.SetResult(new List<TreeNode>());
            await _objectUnderTest.LoadingTask;
            _objectUnderTest.Refresh();

            Assert.AreEqual(2, _objectUnderTest.LoadDataOverrideCallCount);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_loadingPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestRefreshLoadError()
        {
            _objectUnderTest.IsExpanded = true;
            _objectUnderTest.LoadDataSource.SetResult(new List<TreeNode>());
            await _objectUnderTest.LoadingTask;
            _objectUnderTest.Refresh();
            _objectUnderTest.LoadDataSource.SetException(new CloudExplorerSourceException(MockExceptionMessage));
            await _objectUnderTest.LoadingTask;

            Assert.AreEqual(2, _objectUnderTest.LoadDataOverrideCallCount);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_errorPlaceholder, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestRefreshLoadNoItems()
        {
            _objectUnderTest.IsExpanded = true;
            _objectUnderTest.LoadDataSource.SetException(new CloudExplorerSourceException(MockExceptionMessage));
            await _objectUnderTest.LoadingTask;
            _objectUnderTest.Refresh();
            _objectUnderTest.LoadDataSource.SetResult(new List<TreeNode> { s_childNode });
            await _objectUnderTest.LoadingTask;

            Assert.AreEqual(2, _objectUnderTest.LoadDataOverrideCallCount);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_childNode, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestRefreshLoadSuccess()
        {
            _objectUnderTest.IsExpanded = true;
            _objectUnderTest.LoadDataSource.SetException(new CloudExplorerSourceException(MockExceptionMessage));
            await _objectUnderTest.LoadingTask;
            _objectUnderTest.Refresh();
            _objectUnderTest.LoadDataSource.SetResult(new List<TreeNode> { s_childNode });
            await _objectUnderTest.LoadingTask;

            Assert.AreEqual(2, _objectUnderTest.LoadDataOverrideCallCount);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_childNode, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestDoubleRefresh()
        {
            _objectUnderTest.IsExpanded = true;
            _objectUnderTest.LoadDataSource.SetResult(new List<TreeNode>());
            await _objectUnderTest.LoadingTask;
            _objectUnderTest.Refresh();
            _objectUnderTest.Refresh();
            _objectUnderTest.LoadDataSource.SetResult(new List<TreeNode> { s_childNode });
            await _objectUnderTest.LoadingTask;
            _objectUnderTest.LoadDataSource.SetException(new CloudExplorerSourceException(MockExceptionMessage));
            await _objectUnderTest.LoadingTask;

            Assert.AreEqual(2, _objectUnderTest.LoadDataOverrideCallCount);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_childNode, _objectUnderTest.Children[0]);
        }

        [TestMethod]
        public async Task TestRefreshAgain()
        {
            _objectUnderTest.IsExpanded = true;
            _objectUnderTest.LoadDataSource.SetException(new CloudExplorerSourceException(MockExceptionMessage));
            await _objectUnderTest.LoadingTask;
            _objectUnderTest.Refresh();
            _objectUnderTest.LoadDataSource.SetResult(new List<TreeNode>());
            await _objectUnderTest.LoadingTask;
            _objectUnderTest.Refresh();
            _objectUnderTest.LoadDataSource.SetResult(new List<TreeNode> { s_childNode });
            await _objectUnderTest.LoadingTask;

            Assert.AreEqual(3, _objectUnderTest.LoadDataOverrideCallCount);
            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            Assert.AreEqual(s_childNode, _objectUnderTest.Children[0]);
        }
    }
}