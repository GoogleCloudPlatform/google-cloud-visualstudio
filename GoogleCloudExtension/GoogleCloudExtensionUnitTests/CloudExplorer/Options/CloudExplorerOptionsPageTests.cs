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

using GoogleCloudExtension.CloudExplorer.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace GoogleCloudExtensionUnitTests.CloudExplorer.Options
{
    [TestClass]
    public class CloudExplorerOptionsPageTests
    {
        private Mock<CollectionView> _itemsViewMock;
        private Mock<IEditableCollectionView> _editableViewMock;
        private CloudExplorerOptionsPage _objectUnderTest;

        [TestInitialize]
        public void BeforeEach()
        {
            _itemsViewMock = new Mock<CollectionView>(Enumerable.Empty<object>());
            _itemsViewMock.Setup(v => v.DeferRefresh()).Returns(Mock.Of<IDisposable>());
            _editableViewMock = _itemsViewMock.As<IEditableCollectionView>();
            _objectUnderTest = new CloudExplorerOptionsPage(Mock.Of<ICloudExplorerOptions>());
            _objectUnderTest._pubSubFilters.ItemsSource = _itemsViewMock.Object;
        }

        [TestMethod]
        public void TestOnDialogKeyPending()
        {
            _editableViewMock.SetupGet(v => v.IsEditingItem).Returns(false);
            _editableViewMock.SetupGet(v => v.IsAddingNew).Returns(false);

            var args = new RoutedEventArgs(UIElementDialogPage.DialogKeyPendingEvent);
            _objectUnderTest.RaiseEvent(args);

            Assert.IsFalse(args.Handled);
        }

        [TestMethod]
        public void TestOnDialogKeyPendingAddingNew()
        {
            _editableViewMock.SetupGet(v => v.IsEditingItem).Returns(true);
            _editableViewMock.SetupGet(v => v.IsAddingNew).Returns(false);

            var args = new RoutedEventArgs(UIElementDialogPage.DialogKeyPendingEvent);
            _objectUnderTest.RaiseEvent(args);

            Assert.IsTrue(args.Handled);
        }

        [TestMethod]
        public void TestOnDialogKeyEditing()
        {
            _editableViewMock.SetupGet(v => v.IsEditingItem).Returns(false);
            _editableViewMock.SetupGet(v => v.IsAddingNew).Returns(true);

            var args = new RoutedEventArgs(UIElementDialogPage.DialogKeyPendingEvent);
            _objectUnderTest.RaiseEvent(args);

            Assert.IsTrue(args.Handled);
        }
    }
}
