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

using GoogleCloudExtension.Utils.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.Utils.Validation
{
    /// <summary>
    /// Summary description for ValidatingViewModelBaseTests
    /// </summary>
    [TestClass]
    public class ValidatingViewModelBaseTests
    {
        private const string TestPropertyName = "test property";

        private static readonly ValidationResult s_invalidTestResult = new ValidationResult(false, "message");

        private static readonly ValidationResult s_validTestResult = new ValidationResult(true, "second message");

        private static readonly List<ValidationResult> s_invalidResults = new List<ValidationResult> { s_invalidTestResult };

        private static readonly List<ValidationResult> s_validTestResults =
            new List<ValidationResult> { s_validTestResult };

        private TestModel _testObject;
        private List<string> _errorsChangedProperties;
        private List<string> _propertiesChanged;

        [TestInitialize]
        public void InitializeForTest()
        {
            _testObject = new TestModel();
            _errorsChangedProperties = new List<string>();
            _propertiesChanged = new List<string>();
            _testObject.ErrorsChanged += (sender, args) => _errorsChangedProperties.Add(args.PropertyName);
            _testObject.PropertyChanged += (sender, args) => _propertiesChanged.Add(args.PropertyName);
        }

        [TestMethod]
        public void TestInitialConditions()
        {
            Assert.AreEqual(Task.CompletedTask, _testObject.LatestDelayedValidationUpdateTask);
            Assert.IsFalse(_testObject.HasErrors);
            CollectionAssert.That.IsEmpty(_testObject.GetErrors(null).ToList());
            Assert.AreEqual(0, _testObject.HasErrorChangedCallCount);
        }

        [TestMethod]
        public void TestSetValidationResults_WithPendingErrorsRaisesHasErrorsPropertyChanged()
        {
            _testObject.Delay = -1;
            _testObject.SetValidationResults(s_invalidResults, TestPropertyName);

            CollectionAssert.Contains(_propertiesChanged, nameof(_testObject.HasErrors));
        }

        [TestMethod]
        public async Task TestSetValidationResults_WithPendingErrorsRaisesErrorsChangedForProperty()
        {
            _testObject.Delay = 0;

            _testObject.SetValidationResults(s_invalidResults, TestPropertyName);
            await _testObject.LatestDelayedValidationUpdateTask;

            CollectionAssert.Contains(_errorsChangedProperties, TestPropertyName);
        }

        [TestMethod]
        public async Task TestSetValidationResults_UsesCallerMemberName()
        {
            _testObject.Delay = 0;

            _testObject.SetValidationResultsProperty = s_invalidResults;
            await _testObject.LatestDelayedValidationUpdateTask;

            CollectionAssert.Contains(_errorsChangedProperties, nameof(_testObject.SetValidationResultsProperty));
        }

        [TestMethod]
        public void TestHasErrors_WithPendingErrorsReturnsTrue()
        {
            _testObject.Delay = -1;
            _testObject.SetValidationResults(s_invalidResults, TestPropertyName);

            Assert.IsTrue(_testObject.HasErrors);
        }

        [TestMethod]
        public void TestPropertyHasErrors_WithPendingErrorsReturnsTrue()
        {
            _testObject.Delay = -1;
            _testObject.SetValidationResults(s_invalidResults, TestPropertyName);

            Assert.IsTrue(_testObject.PropertyHasErrors(TestPropertyName));
        }

        [TestMethod]
        public void TestGetErrors_DoesNotReturnPendingErrors()
        {
            _testObject.Delay = -1;
            _testObject.SetValidationResults(s_invalidResults, TestPropertyName);

            CollectionAssert.That.IsEmpty(_testObject.GetErrors(TestPropertyName));
        }

        [TestMethod]
        public async Task TestGetErrors_ReturnsDelayedErrors()
        {
            _testObject.Delay = 0;

            _testObject.SetValidationResults(s_invalidResults, TestPropertyName);
            await _testObject.LatestDelayedValidationUpdateTask;
            IEnumerable<ValidationResult> results = _testObject.GetErrors(TestPropertyName);

            CollectionAssert.AreEqual(s_invalidResults, results.ToList());
        }

        [TestMethod]
        public void TestSetValidationResults_DoesNotDelayValidResults()
        {
            _testObject.Delay = -1;
            _testObject.SetValidationResults(s_validTestResults, TestPropertyName);

            CollectionAssert.AreEqual(s_validTestResults, _testObject.GetErrors(TestPropertyName).ToList());
        }

        [TestMethod]
        public void TestHasErrors_UsesNewerResults()
        {
            _testObject.Delay = -1;
            _testObject.SetValidationResults(s_invalidResults, TestPropertyName);
            _testObject.SetValidationResults(s_validTestResults, TestPropertyName);

            Assert.IsFalse(_testObject.HasErrors);
        }

        [TestMethod]
        public void TestPropertyHasErrors_UsesNewerResults()
        {
            _testObject.Delay = -1;
            _testObject.SetValidationResults(s_invalidResults, TestPropertyName);
            _testObject.SetValidationResults(s_validTestResults, TestPropertyName);

            Assert.IsFalse(_testObject.PropertyHasErrors(TestPropertyName));
        }

        [TestMethod]
        public async Task TestSetValidationResults_PendingInvalidResultsDoNotOverwriteNewerValidResults()
        {
            _testObject.Delay = 200;
            _testObject.SetValidationResults(s_invalidResults, TestPropertyName);
            Task invalidPendingTask = _testObject.LatestDelayedValidationUpdateTask;
            _testObject.SetValidationResults(s_validTestResults, TestPropertyName);
            await invalidPendingTask;

            Assert.IsFalse(_testObject.HasErrors);
            CollectionAssert.AreEqual(s_validTestResults, _testObject.GetErrors(TestPropertyName).ToList());
        }

        [TestMethod]
        public void TestInterfaceGetErrors_SameAsGetErrors()
        {
            _testObject.Delay = 0;
            _testObject.SetValidationResults(s_validTestResults, TestPropertyName);

            IEnumerable interfaceErrors = ((INotifyDataErrorInfo)_testObject).GetErrors(TestPropertyName);
            CollectionAssert.AreEqual(s_validTestResults, interfaceErrors.Cast<ValidationResult>().ToList());
        }

        [TestMethod]
        public void TestSetValidationResults_CallsHasErrorsChanged()
        {
            _testObject.Delay = -1;
            _testObject.SetValidationResults(s_invalidResults, TestPropertyName);

            Assert.AreEqual(1, _testObject.HasErrorChangedCallCount);
        }

        [TestMethod]
        public void TestSetAndRaiseWithValidations_SetsReferencedValue()
        {
            string storage = null;
            const string expectedValue = "expectedValue";
            _testObject.Delay = 0;
            _testObject.SetAndRaiseWithValidation(ref storage, expectedValue, s_validTestResults, TestPropertyName);

            Assert.AreEqual(expectedValue, storage);
        }

        [TestMethod]
        public void TestSetAndRaiseWithValidations_CallsSetValidationResults()
        {
            string storage = null;
            const string value = "value";
            _testObject.Delay = 0;
            _testObject.SetAndRaiseWithValidation(ref storage, value, s_validTestResults, TestPropertyName);

            Assert.AreEqual(1, _testObject.HasErrorChangedCallCount);
        }


        [TestMethod]
        public void TestSetAndRaiseWithValidations_UsesCallerMemberName()
        {
            _testObject.Delay = 0;
            _testObject.SetAndRaiseWithValidationProperty = 0;

            CollectionAssert.Contains(_errorsChangedProperties, nameof(_testObject.SetAndRaiseWithValidationProperty));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), AllowDerivedTypes = true)]
        public void TestSetValidationResults_GivenNullPropertyThrows()
        {
            _testObject.SetValidationResults(s_invalidResults, null);
        }

        private class TestModel : ValidatingViewModelBase
        {
            private int _intField;

            public int Delay
            {
                set => MillisecondsDelay = value;
            }

            public IEnumerable<ValidationResult> SetValidationResultsProperty
            {
                set => SetValidationResults(value);
            }

            public int SetAndRaiseWithValidationProperty
            {
                set => SetAndRaiseWithValidation(ref _intField, value, Enumerable.Empty<ValidationResult>());
            }

            public new void SetValidationResults(IEnumerable<ValidationResult> results, string propertyName) =>
                base.SetValidationResults(results, propertyName);

            public new void SetAndRaiseWithValidation<T>(
                ref T storage,
                T value,
                IEnumerable<ValidationResult> validations,
                string propertyName)
            {
                base.SetAndRaiseWithValidation(ref storage, value, validations, propertyName);
            }

            protected override void HasErrorsChanged() => HasErrorChangedCallCount++;

            public int HasErrorChangedCallCount { get; private set; } = 0;
        }
    }
}
