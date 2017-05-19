using GoogleCloudExtension.Utils.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        private TestModel _testObject;
        private readonly ValidationResult _testResult = new ValidationResult(false, "message");
        private readonly ValidationResult _secondTestResult = new ValidationResult(false, "second message");
        private List<ValidationResult> _testResults;
        private List<ValidationResult> _secondTestResults;
        private TaskCompletionSource<string> _errorsChangedSource;

        private class TestModel : ValidatingViewModelBase
        {
            public int Delay
            {
                set { MillisecondsDelay = value; }
            }

            public IEnumerable<ValidationResult> SetPropertyErrors
            {
                set
                {
                    SetValidationResults(value);
                }
            }

            public void SetValidationResultsProxy(IEnumerable<ValidationResult> results, string propertyName)
            {
                SetValidationResults(results, propertyName);
            }

            protected override void HasErrorsChanged()
            {
                HasErrorChangedCount++;
            }

            public int HasErrorChangedCount { get; private set; } = 0;
        }

        public ValidatingViewModelBaseTests()
        {
            _testResults = new List<ValidationResult>
            {
                _testResult
            };
            _secondTestResults = new List<ValidationResult>
            {
                _secondTestResult
            };
        }

        [TestInitialize]
        public void InitializeForTest()
        {
            _testObject = new TestModel();
            _errorsChangedSource = new TaskCompletionSource<string>();
            _testObject.ErrorsChanged += (sender, args) => _errorsChangedSource.SetResult(args.PropertyName);
        }

        [TestMethod]
        public void TestInitalConditions()
        {
            const string propertyName = nameof(_testObject.SetPropertyErrors);

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
            const string propertyName = nameof(_testObject.SetPropertyErrors);
            _testObject.Delay = -1;
            _testObject.SetPropertyErrors = _testResults;

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
            _testObject.SetPropertyErrors = _testResults;
            string erroredProperty = await _errorsChangedSource.Task;

            Assert.AreEqual(nameof(_testObject.SetPropertyErrors), erroredProperty);
            CollectionAssert.AreEqual(_testResults, _testObject.GetErrors(erroredProperty).ToList());
            Assert.IsTrue(_testObject.HasErrors);
        }

        [TestMethod]
        public async Task TestSetValidationResults2()
        {
            _testObject.Delay = 0;
            _testObject.SetValidationResultsProxy(_testResults, TestPropertyName);
            string erroredProperty = await _errorsChangedSource.Task;

            Assert.AreEqual(TestPropertyName, erroredProperty);
            CollectionAssert.AreEqual(_testResults, _testObject.GetErrors(erroredProperty).ToList());
            Assert.IsTrue(_testObject.HasErrors);
        }

        [TestMethod]
        public async Task TestInterfaceGetErrors()
        {
            _testObject.Delay = 0;
            _testObject.SetPropertyErrors = _testResults;
            string erroredProperty = await _errorsChangedSource.Task;
            _errorsChangedSource = new TaskCompletionSource<string>();
            _testObject.SetValidationResultsProxy(_secondTestResults, TestPropertyName);
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
            _testObject.SetPropertyErrors = _testResults;
            string firstErroredProperty = await _errorsChangedSource.Task;
            _errorsChangedSource = new TaskCompletionSource<string>();
            _testObject.SetValidationResultsProxy(_secondTestResults, TestPropertyName);
            string secondErroredProperty = await _errorsChangedSource.Task;

            CollectionAssert.AreEqual(_testResults, _testObject.GetErrors(firstErroredProperty).ToList());
            CollectionAssert.AreEqual(_secondTestResults, _testObject.GetErrors(secondErroredProperty).ToList());
            CollectionAssert.AreEqual(_testResults.Concat(_secondTestResults).ToList(), _testObject.GetErrors(null).ToList());
        }

        [TestMethod]
        public void TestHasErrors()
        {
            _testObject.Delay = -1;
            _testObject.SetPropertyErrors = _testResults;

            Assert.IsTrue(_testObject.HasErrors);
            Assert.IsFalse(_errorsChangedSource.Task.IsCompleted);
        }

        [TestMethod]
        public void TestHasErrorsChanged()
        {
            _testObject.Delay = -1;
            _testObject.SetValidationResultsProxy(_testResults, TestPropertyName);

            Assert.AreEqual(1, _testObject.HasErrorChangedCount);
            Assert.IsFalse(_errorsChangedSource.Task.IsCompleted);
        }
    }
}
