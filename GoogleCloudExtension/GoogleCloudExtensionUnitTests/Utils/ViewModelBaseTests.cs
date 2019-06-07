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

using System;
using System.Collections.Generic;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleCloudExtensionUnitTests.Utils
{
    [TestClass]
    public class ViewModelBaseTests
    {
        private TestViewModelBase _objectUnderTest;
        private List<string> _changedProperties;

        [TestInitialize]
        public void BeforeEach()
        {
            _objectUnderTest = new TestViewModelBase();

            _changedProperties = new List<string>();
            _objectUnderTest.PropertyChanged += (sender, args) => _changedProperties.Add(args.PropertyName);
        }

        [TestMethod]
        public void TestLoading_SetsProperty()
        {
            _objectUnderTest.Loading = true;

            Assert.IsTrue(_objectUnderTest.Loading);
        }

        [TestMethod]
        public void TestLoading_NotifiesPropertyChanged()
        {
            _objectUnderTest.Loading = true;

            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.Loading));
        }

        [TestMethod]
        public void TestLoadingMessage_SetsProperty()
        {
            const string expectedLoadingMessage = "Expected Loading Message";

            _objectUnderTest.LoadingMessage = expectedLoadingMessage;

            Assert.AreEqual(expectedLoadingMessage, _objectUnderTest.LoadingMessage);
        }

        [TestMethod]
        public void TestLoadingMessage_NotifiesPropertyChanged()
        {
            _objectUnderTest.LoadingMessage = "Loading Message";

            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.LoadingMessage));
        }

        [TestMethod]
        public void TestResult_SetsProperty()
        {
            const string expectedResult = "Expected Loading Message";

            _objectUnderTest.ResultOverride = expectedResult;

            Assert.AreEqual(expectedResult, _objectUnderTest.Result);
        }

        [TestMethod]
        public void TestResult_NotifiesPropertyChanged()
        {
            _objectUnderTest.ResultOverride = "Loading Message";

            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.Result));
        }

        private class TestViewModelBase : ViewModelBase<object>
        {
            public sealed override event Action Close
            {
                add { }
                remove { }
            }

            public object ResultOverride
            {
                set => Result = value;
            }
        }
    }
}
