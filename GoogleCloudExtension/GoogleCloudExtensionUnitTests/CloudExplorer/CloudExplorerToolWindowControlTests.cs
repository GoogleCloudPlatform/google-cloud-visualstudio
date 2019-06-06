// Copyright 2019 Google Inc. All Rights Reserved.
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

using System;
using System.Windows;
using System.Windows.Controls;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace GoogleCloudExtensionUnitTests.CloudExplorer
{
    [TestClass]
    public class CloudExplorerToolWindowControlTests
    {
        private CloudExplorerToolWindowControl _objectUnderTest;
        private Mock<ISelectionUtils> _selectionUtilsMock;

        [TestInitialize]
        public void BeforeEach()
        {
            Application.Current.Resources.MergedDictionaries.Add(ResourceDictionary);

            _selectionUtilsMock = new Mock<ISelectionUtils>();
            _selectionUtilsMock.Setup(s => s.SelectItemAsync(It.IsAny<object>())).Returns(Task.CompletedTask);
            _selectionUtilsMock.Setup(s => s.ClearSelectionAsync()).Returns(Task.CompletedTask);
            _objectUnderTest = new CloudExplorerToolWindowControl(_selectionUtilsMock.Object);
        }

        [TestCleanup]
        public void AfterEach()
        {
            Application.Current.Resources.MergedDictionaries.Remove(ResourceDictionary);
        }

        private ResourceDictionary ResourceDictionary { get; } = new ResourceDictionary
        {
            [VsResourceKeys.ButtonStyleKey] = new Style(typeof(Button))
        };

        [TestMethod]
        public void TestTreeView_SelectedItemChanged_NotifiesSelectionUtils()
        {
            ICloudExplorerItemSource newSelectedSource = Mock.Of<ICloudExplorerItemSource>();

            var newItemSource =
                new RoutedPropertyChangedEventArgs<object>(null, newSelectedSource)
                {
                    RoutedEvent = TreeView.SelectedItemChangedEvent
                };
            _objectUnderTest._treeView.RaiseEvent(newItemSource);

            _selectionUtilsMock.Verify(s => s.SelectItemAsync(newSelectedSource.Item), Times.Once);
        }

        [TestMethod]
        public void TestTreeView_SelectedItemChanged_RegistersOnItemChanged()
        {
            var itemSource = Mock.Of<ICloudExplorerItemSource>();
            var selectedItemSetEventArgs =
                new RoutedPropertyChangedEventArgs<object>(null, itemSource)
                {
                    RoutedEvent = TreeView.SelectedItemChangedEvent
                };
            _objectUnderTest._treeView.RaiseEvent(selectedItemSetEventArgs);
            var itemChangedArgs = new EventArgs();
            Mock.Get(itemSource).Raise(i => i.ItemChanged += null, itemSource, itemChangedArgs);

            _selectionUtilsMock.Verify(s => s.SelectItemAsync(itemSource.Item), Times.Exactly(2));
        }

        [TestMethod]
        public void TestTreeView_SelectedItemChanged_UnregistersOnItemChanged()
        {
            var itemSource = Mock.Of<ICloudExplorerItemSource>();
            var selectedItemSetEventArgs =
                new RoutedPropertyChangedEventArgs<object>(null, itemSource)
                {
                    RoutedEvent = TreeView.SelectedItemChangedEvent
                };
            var selectedItemClearedEventArgs = new RoutedPropertyChangedEventArgs<object>(itemSource, null)
            {
                RoutedEvent = TreeView.SelectedItemChangedEvent
            };
            _objectUnderTest._treeView.RaiseEvent(selectedItemSetEventArgs);
            _objectUnderTest._treeView.RaiseEvent(selectedItemClearedEventArgs);

            _selectionUtilsMock.Verify(s => s.SelectItemAsync(itemSource.Item), Times.Once);

            var itemChangedArgs = new EventArgs();
            Mock.Get(itemSource).Raise(i => i.ItemChanged += null, itemSource, itemChangedArgs);

            _selectionUtilsMock.Verify(s => s.SelectItemAsync(itemSource.Item), Times.Once);
        }

        [TestMethod]
        public void TestTreeView_SelectedItemChanged_Clears()
        {
            var oldItemSource = Mock.Of<ICloudExplorerItemSource>();
            _objectUnderTest._treeView.RaiseEvent(new RoutedPropertyChangedEventArgs<object>(oldItemSource, null)
            {
                RoutedEvent = TreeView.SelectedItemChangedEvent
            });

            _selectionUtilsMock.Verify(s => s.ClearSelectionAsync(), Times.Once);
        }
    }
}
