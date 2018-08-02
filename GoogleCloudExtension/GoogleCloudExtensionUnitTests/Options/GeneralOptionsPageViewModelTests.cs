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

using GoogleCloudExtension.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace GoogleCloudExtensionUnitTests.Options
{
    [TestClass]
    public class GeneralOptionsPageViewModelTests
    {
        private GeneralOptionsPageViewModel _objectUnderTest;
        private List<string> _changedProperties;

        [TestInitialize]
        public void BeforeEach()
        {
            _objectUnderTest = new GeneralOptionsPageViewModel();

            _changedProperties = new List<string>();
            _objectUnderTest.PropertyChanged += (sender, args) => _changedProperties.Add(args.PropertyName);
        }

        [TestMethod]
        public void TestInitialConditions()
        {
            _objectUnderTest = new GeneralOptionsPageViewModel();

            Assert.IsFalse(_objectUnderTest.OptIn);
            Assert.IsFalse(_objectUnderTest.HideUserProjectControl);
            Assert.IsNotNull(_objectUnderTest.AnalyticsLearnMoreLinkCommand);
        }

        [TestMethod]
        public void TestSetOptIn_SetsProperty()
        {
            _objectUnderTest.OptIn = true;

            Assert.IsTrue(_objectUnderTest.OptIn);
        }

        [TestMethod]
        public void TestSetOptIn_RaisesPropertyChanged()
        {
            _objectUnderTest.OptIn = true;

            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.OptIn));
        }

        [TestMethod]
        public void TestSetHideUserProjectControl_SetsProperty()
        {
            _objectUnderTest.HideUserProjectControl = true;

            Assert.IsTrue(_objectUnderTest.HideUserProjectControl);
        }

        [TestMethod]
        public void TestSetHideUserProjectControl_RaisesPropertyChanged()
        {
            _objectUnderTest.HideUserProjectControl = true;

            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.HideUserProjectControl));
        }
    }
}
