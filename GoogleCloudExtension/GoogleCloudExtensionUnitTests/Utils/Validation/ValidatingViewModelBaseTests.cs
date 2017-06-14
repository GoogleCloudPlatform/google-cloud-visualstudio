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

namespace GoogleCloudExtensionUnitTests.Utils.Validation
{
    /// <summary>
    /// Summary description for ValidatingViewModelBaseTests
    /// </summary>
    [TestClass]
    public class ValidatingViewModelBaseTests
    {
        private const string TestPropertyName = "test property";

        private static readonly ValidationResult s_testResult = new ValidationResult(false, "message");

        private static readonly ValidationResult s_secondTestResult = new ValidationResult(false, "second message");

        private static readonly ValidationResult s_setTestValidation = new ValidationResult(false, "second message");

        private static readonly List<ValidationResult> s_testResults = new List<ValidationResult> { s_testResult };

        private static readonly List<ValidationResult> s_secondTestResults =
            new List<ValidationResult> { s_secondTestResult };

        private static readonly List<ValidationResult> s_setTestValidations =
            new List<ValidationResult> { s_setTestValidation };

        private TestModel _testObject;

        private TaskCompletionSource<string> _errorsChangedSource;
        private int _totalErrorsChanged;

        [TestInitialize]
        public void InitializeForTest()
        {
            _testObject = new TestModel();
            _errorsChangedSource = new TaskCompletionSource<string>();
            _totalErrorsChanged = 0;
            _testObject.ErrorsChanged += (sender, args) =>
            {
                _errorsChangedSource.SetResult(args.PropertyName);
                _totalErrorsChanged++;
            };
        }

        [TestMethod]
        public void TestInitialConditions()
        {
            const string propertyName = nameof(_testObject.ErrorsProperty);

            Assert.IsFalse(_testObject.HasErrors);
            Assert.IsFalse(_testObject.GetErrors(propertyName).Any());
            Assert.IsFalse(((INotifyDataErrorInfo)_testObject).GetErrors(propertyName).GetEnumerator().MoveNext());
            Assert.IsFalse(_testObject.GetErrors(null).Any());
            Assert.IsFalse(((INotifyDataErrorInfo)_testObject).GetErrors(null).GetEnumerator().MoveNext());
            Assert.AreEqual(0, _testObject.HasErrorChangedCount);
        }

        [TestMethod]
        public void TestUnfufilledConditions()
        {
            const string propertyName = nameof(_testObject.ErrorsProperty);
            _testObject.Delay = -1;
            _testObject.ErrorsProperty = s_testResults;

            Assert.IsFalse(_testObject.GetErrors(propertyName).Any());
            Assert.IsFalse(((INotifyDataErrorInfo)_testObject).GetErrors(propertyName).GetEnumerator().MoveNext());
            Assert.IsFalse(_testObject.GetErrors(null).Any());
            Assert.IsFalse(((INotifyDataErrorInfo)_testObject).GetErrors(null).GetEnumerator().MoveNext());
            Assert.IsFalse(_errorsChangedSource.Task.IsCompleted);
        }

        [TestMethod]
        public async Task TestSetValidationResults1()
        {
            _testObject.Delay = 0;
            _testObject.ErrorsProperty = s_testResults;
            string erroredProperty = await _errorsChangedSource.Task;

            Assert.AreEqual(nameof(_testObject.ErrorsProperty), erroredProperty);
            CollectionAssert.AreEqual(s_testResults, _testObject.GetErrors(erroredProperty).ToList());
            Assert.IsTrue(_testObject.HasErrors);
        }

        [TestMethod]
        public async Task TestSetValidationResults2()
        {
            _testObject.Delay = 0;
            _testObject.SetValidationResultsProxy(s_testResults, TestPropertyName);
            string erroredProperty = await _errorsChangedSource.Task;

            Assert.AreEqual(TestPropertyName, erroredProperty);
            CollectionAssert.AreEqual(s_testResults, _testObject.GetErrors(erroredProperty).ToList());
            Assert.IsTrue(_testObject.HasErrors);
        }

        [TestMethod]
        public async Task TestSetValidationResults3()
        {
            _testObject.Delay = -1;
            _testObject.SetValidationResultsProxy(null, TestPropertyName);
            Assert.IsTrue(_errorsChangedSource.Task.IsCompleted);
            string erroredProperty = await _errorsChangedSource.Task;

            Assert.AreEqual(TestPropertyName, erroredProperty);
            CollectionAssert.AreEqual(new object[0], _testObject.GetErrors(erroredProperty).ToList());
            Assert.IsFalse(_testObject.HasErrors);
        }

        [TestMethod]
        public async Task TestSetValidationResults4()
        {
            _testObject.Delay = 500;
            _testObject.SetValidationResultsProxy(s_testResults, TestPropertyName);
            await Task.Delay(100);
            Assert.IsFalse(_errorsChangedSource.Task.IsCompleted);
            _testObject.SetValidationResultsProxy(null, TestPropertyName);
            string erroredProperty = await _errorsChangedSource.Task;
            _errorsChangedSource = new TaskCompletionSource<string>();

            Task first = await Task.WhenAny(_errorsChangedSource.Task, Task.Delay(600));
            Assert.AreNotEqual(_errorsChangedSource.Task, first);
            Assert.AreEqual(1, _totalErrorsChanged);
            Assert.AreEqual(TestPropertyName, erroredProperty);
            CollectionAssert.AreEqual(new object[0], _testObject.GetErrors(erroredProperty).ToList());
            Assert.IsFalse(_testObject.HasErrors);
        }

        [TestMethod]
        public async Task TestInterfaceGetErrors()
        {
            _testObject.Delay = 0;
            _testObject.ErrorsProperty = s_testResults;
            string erroredProperty = await _errorsChangedSource.Task;
            _errorsChangedSource = new TaskCompletionSource<string>();
            _testObject.SetValidationResultsProxy(s_secondTestResults, TestPropertyName);
            string secondErroredProperty = await _errorsChangedSource.Task;

            IEnumerable interfaceErrors = ((INotifyDataErrorInfo)_testObject).GetErrors(erroredProperty);
            Assert.IsInstanceOfType(interfaceErrors, typeof(IEnumerable<ValidationResult>));
            CollectionAssert.AreEqual(
                _testObject.GetErrors(erroredProperty).ToList(),
                ((IEnumerable<ValidationResult>)interfaceErrors).ToList());

            IEnumerable secondInterfaceErrors = ((INotifyDataErrorInfo)_testObject).GetErrors(secondErroredProperty);
            Assert.IsInstanceOfType(secondInterfaceErrors, typeof(IEnumerable<ValidationResult>));
            CollectionAssert.AreEqual(
                _testObject.GetErrors(secondErroredProperty).ToList(),
                ((IEnumerable<ValidationResult>)secondInterfaceErrors).ToList());

            IEnumerable allInterfaceErrors = ((INotifyDataErrorInfo)_testObject).GetErrors(null);
            Assert.IsInstanceOfType(allInterfaceErrors, typeof(IEnumerable<ValidationResult>));
            CollectionAssert.AreEqual(
                _testObject.GetErrors(null).ToList(), ((IEnumerable<ValidationResult>)allInterfaceErrors).ToList());
        }

        [TestMethod]
        public async Task TestGetErrors()
        {
            _testObject.Delay = 0;
            _testObject.ErrorsProperty = s_testResults;
            string firstErroredProperty = await _errorsChangedSource.Task;
            _errorsChangedSource = new TaskCompletionSource<string>();
            _testObject.SetValidationResultsProxy(s_secondTestResults, TestPropertyName);
            string secondErroredProperty = await _errorsChangedSource.Task;

            CollectionAssert.AreEqual(s_testResults, _testObject.GetErrors(firstErroredProperty).ToList());
            CollectionAssert.AreEqual(s_secondTestResults, _testObject.GetErrors(secondErroredProperty).ToList());
            CollectionAssert.AreEqual(s_testResults.Concat(s_secondTestResults).ToList(), _testObject.GetErrors(null).ToList());
        }

        [TestMethod]
        public void TestHasErrors()
        {
            _testObject.Delay = -1;
            _testObject.ErrorsProperty = s_testResults;

            Assert.IsTrue(_testObject.HasErrors);
            Assert.IsFalse(_errorsChangedSource.Task.IsCompleted);
        }

        [TestMethod]
        public void TestHasErrorsChanged()
        {
            _testObject.Delay = -1;
            _testObject.SetValidationResultsProxy(s_testResults, TestPropertyName);

            Assert.AreEqual(1, _testObject.HasErrorChangedCount);
            Assert.IsFalse(_errorsChangedSource.Task.IsCompleted);
        }

        [TestMethod]
        public async Task TestSetAndRaiseWithValidations()
        {
            _testObject.Delay = 0;
            _testObject.SomeIntProperty = int.MaxValue;
            string changedProperty = await _errorsChangedSource.Task;

            Assert.AreEqual(nameof(_testObject.SomeIntProperty), changedProperty);
            Assert.AreEqual(int.MaxValue, _testObject.SomeIntProperty);
            CollectionAssert.AreEqual(s_setTestValidations, _testObject.GetErrors(changedProperty).ToList());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), AllowDerivedTypes = true)]
        public void TestSetValidationResultsNullProperty()
        {
            _testObject.SetValidationResultsProxy(s_testResults, null);
        }

        private class TestModel : ValidatingViewModelBase
        {
            private int _someIntProperty;

            public int Delay
            {
                set { MillisecondsDelay = value; }
            }

            public IEnumerable<ValidationResult> ErrorsProperty
            {
                set
                {
                    SetValidationResults(value);
                }
            }

            public int SomeIntProperty
            {
                get { return _someIntProperty; }
                set { SetAndRaiseWithValidation(ref _someIntProperty, value, s_setTestValidations); }
            }

            public int HasErrorChangedCount { get; private set; } = 0;

            public void SetValidationResultsProxy(IEnumerable<ValidationResult> results, string propertyName)
            {
                SetValidationResults(results, propertyName);
            }

            protected override void HasErrorsChanged()
            {
                HasErrorChangedCount++;
            }
        }
    }
}
