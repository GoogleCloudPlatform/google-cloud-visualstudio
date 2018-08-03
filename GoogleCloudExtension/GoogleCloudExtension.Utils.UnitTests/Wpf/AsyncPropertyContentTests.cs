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

using GoogleCloudExtension.Utils.Async;
using GoogleCloudExtension.Utils.Wpf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GoogleCloudExtension.Utils.UnitTests.Wpf
{
    [TestClass]
    public class AsyncPropertyContentTests
    {
        private const string ExpectedContent = "Expected Content";
        private AsyncPropertyContent _objectUnderTest;

        [TestInitialize]
        public void BeforeEach()
        {
            _objectUnderTest = new AsyncPropertyContent();
        }

        [TestMethod]
        public void TestTarget_SetsProperty()
        {
            var expectedTarget = new AsyncProperty();

            _objectUnderTest.Target = expectedTarget;

            Assert.AreEqual(expectedTarget, _objectUnderTest.Target);
        }

        [TestMethod]
        public void TestTarget_SetsContent()
        {
            _objectUnderTest.SuccessContent = ExpectedContent;
            _objectUnderTest.Target = new AsyncProperty(Task.CompletedTask);

            Assert.AreEqual(ExpectedContent, _objectUnderTest.Content);
        }

        [TestMethod]
        public void TestTarget_LeavesContentUnchangedWhenInvalidTask()
        {
            _objectUnderTest.CanceledContent = ExpectedContent;
            _objectUnderTest.Target = new AsyncProperty(Task.FromCanceled(new CancellationToken(true)));
            _objectUnderTest.Target = new AsyncProperty(null);

            Assert.AreEqual(ExpectedContent, _objectUnderTest.Content);
        }

        [TestMethod]
        public void TestTarget_LeavesContentUnchangedWhenNull()
        {
            _objectUnderTest.CanceledContent = ExpectedContent;
            _objectUnderTest.Target = new AsyncProperty(Task.FromCanceled(new CancellationToken(true)));
            _objectUnderTest.Target = null;

            Assert.AreEqual(ExpectedContent, _objectUnderTest.Content);
        }

        [TestMethod]
        public async Task TestTarget_RegistersOnPropertyChangedSetsContent()
        {
            var source = new TaskCompletionSource<string>();
            _objectUnderTest.SuccessContent = ExpectedContent;
            _objectUnderTest.PendingContent = new object();

            _objectUnderTest.Target = new AsyncProperty<string>(source.Task);
            source.SetResult("");
            await _objectUnderTest.Target;

            Assert.AreEqual(ExpectedContent, _objectUnderTest.Content);
        }

        [TestMethod]
        public async Task TestTarget_UnregistersOnPropertyChanged()
        {
            var oldPropertySource = new TaskCompletionSource<string>();
            var newPropertySource = new TaskCompletionSource<string>();
            _objectUnderTest.CanceledContent = ExpectedContent;
            _objectUnderTest.SuccessContent = new object();
            var oldProperty = new AsyncProperty(oldPropertySource.Task);
            var newProperty = new AsyncProperty(newPropertySource.Task);

            _objectUnderTest.Target = oldProperty;
            _objectUnderTest.Target = newProperty;
            newPropertySource.SetCanceled();
            await _objectUnderTest.Target;
            oldPropertySource.SetResult("");
            await oldProperty;

            Assert.AreEqual(ExpectedContent, _objectUnderTest.Content);
        }

        [TestMethod]
        public void TestSuccessContent_SetsProperty()
        {
            _objectUnderTest.SuccessContent = ExpectedContent;

            Assert.AreEqual(ExpectedContent, _objectUnderTest.SuccessContent);
        }

        [TestMethod]
        public void TestSuccessContent_SetsContent()
        {
            _objectUnderTest.Target = new AsyncProperty(Task.CompletedTask);

            _objectUnderTest.SuccessContent = ExpectedContent;

            Assert.AreEqual(ExpectedContent, _objectUnderTest.Content);
        }

        [TestMethod]
        public void TestPendingContent_SetsProperty()
        {
            _objectUnderTest.PendingContent = ExpectedContent;

            Assert.AreEqual(ExpectedContent, _objectUnderTest.PendingContent);
        }

        [TestMethod]
        public void TestPendingContent_SetsContent()
        {
            _objectUnderTest.Target = new AsyncProperty(new TaskCompletionSource<object>().Task);

            _objectUnderTest.PendingContent = ExpectedContent;

            Assert.AreEqual(ExpectedContent, _objectUnderTest.Content);
        }

        [TestMethod]
        public void TestCanceledContent_SetsProperty()
        {
            _objectUnderTest.CanceledContent = ExpectedContent;

            Assert.AreEqual(ExpectedContent, _objectUnderTest.CanceledContent);
        }

        [TestMethod]
        public void TestCanceledContent_SetsContent()
        {
            _objectUnderTest.Target = new AsyncProperty(Task.FromCanceled(new CancellationToken(true)));

            _objectUnderTest.CanceledContent = ExpectedContent;

            Assert.AreEqual(ExpectedContent, _objectUnderTest.Content);
        }

        [TestMethod]
        public void TestErrorContent_SetsProperty()
        {
            _objectUnderTest.ErrorContent = ExpectedContent;

            Assert.AreEqual(ExpectedContent, _objectUnderTest.ErrorContent);
        }

        [TestMethod]
        public void TestErrorContent_SetsContent()
        {
            _objectUnderTest.Target = new AsyncProperty(Task.FromException(new Exception()));

            _objectUnderTest.ErrorContent = ExpectedContent;

            Assert.AreEqual(ExpectedContent, _objectUnderTest.Content);
        }

        [TestMethod]
        public void TestStaticConstructor_OverridesDefaultStyleKey()
        {
            FieldInfo fieldInfo = typeof(FrameworkElement).GetField(
                "DefaultStyleKeyProperty",
                BindingFlags.NonPublic | BindingFlags.Static);
            Debug.Assert(fieldInfo != null);
            var defaultStyleKeyProperty = (DependencyProperty)fieldInfo.GetValue(null);

            object defaultStyleKey = _objectUnderTest.GetValue(defaultStyleKeyProperty);

            Assert.AreEqual(typeof(AsyncPropertyContent), defaultStyleKey);
        }
    }
}
