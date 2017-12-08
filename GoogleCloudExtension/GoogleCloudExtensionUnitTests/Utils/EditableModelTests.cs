using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace GoogleCloudExtensionUnitTests.Utils
{
    /// <summary>
    /// Summary description for EditableModelTests
    /// </summary>
    [TestClass]
    public class EditableModelTests
    {
        private const string InitialValue = "InitialValue";
        private const string ChangedValue = "ChangedValue";

        [TestMethod]
        public void TestDefaultValue()
        {
            var objectUnderTest = new EditableModel<string>();

            Assert.IsNull(objectUnderTest.Value);
        }

        [TestMethod]
        public void TestInitalValue()
        {
            var objectUnderTest = new EditableModel<string>(InitialValue);

            Assert.AreEqual(InitialValue, objectUnderTest.Value);
        }

        [TestMethod]
        public void TestChangedValue()
        {
            var objectUnderTest = new EditableModel<string>(InitialValue);

            objectUnderTest.Value = ChangedValue;

            Assert.AreEqual(ChangedValue, objectUnderTest.Value);
        }

        [TestMethod]
        public void TestCancelWithoutBeginDoesNotRevert()
        {
            var objectUnderTest = new EditableModel<string>(InitialValue);

            objectUnderTest.Value = ChangedValue;
            objectUnderTest.CancelEdit();

            Assert.AreEqual(ChangedValue, objectUnderTest.Value);
        }

        [TestMethod]
        public void TestCancelWithBeginReverts()
        {
            var objectUnderTest = new EditableModel<string>(InitialValue);

            objectUnderTest.BeginEdit();
            objectUnderTest.Value = ChangedValue;
            objectUnderTest.CancelEdit();

            Assert.AreEqual(InitialValue, objectUnderTest.Value);
        }

        [TestMethod]
        public void TestCancelAfterCommitDoesNotRevert()
        {
            var objectUnderTest = new EditableModel<string>(InitialValue);

            objectUnderTest.BeginEdit();
            objectUnderTest.Value = ChangedValue;
            objectUnderTest.EndEdit();
            objectUnderTest.CancelEdit();

            Assert.AreEqual(ChangedValue, objectUnderTest.Value);
        }

        [TestMethod]
        public void TestUpdateValueNotifies()
        {
            var objectUnderTest = new EditableModel<string>(InitialValue);
            var eventHandlerMock = new Mock<Action<object, PropertyChangedEventArgs>>();
            objectUnderTest.PropertyChanged += new PropertyChangedEventHandler(eventHandlerMock.Object);

            objectUnderTest.Value = ChangedValue;

            eventHandlerMock.Verify(
                h => h(
                    objectUnderTest,
                    It.Is<PropertyChangedEventArgs>(args => args.PropertyName == nameof(objectUnderTest.Value))),
                Times.Once);
        }

        [TestMethod]
        public void TestToString()
        {
            const string testString = "test string";
            var objectUnderTest = new EditableModel<object>(Mock.Of<object>(o => o.ToString() == testString));

            string resultString = objectUnderTest.ToString();

            Assert.AreEqual(testString, resultString);
        }

        [TestMethod]
        public void TestToStringNull()
        {
            var objectUnderTest = new EditableModel<object>(null);

            string resultString = objectUnderTest.ToString();

            Assert.AreEqual("", resultString);
        }

        [TestMethod]
        public void TestStaticOf()
        {
            var input = new object();
            EditableModel<object> result = EditableModel.Of(input);

            Assert.AreEqual(input, result.Value);
        }

        [TestMethod]
        public void TestStaticToEditableModelsOnNull()
        {
            IEnumerable<object> input = null;
            // ReSharper disable once ExpressionIsAlwaysNull
            IEnumerable<EditableModel<object>> result = input.ToEditableModels();

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestStaticToEditableModelsOnEmpty()
        {
            IEnumerable<object> input = Enumerable.Empty<object>();

            IEnumerable<EditableModel<object>> result = input.ToEditableModels();

            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void TestStaticToEditableModelsOnArray()
        {
            var input = new[] { 1, 2, 3 };

            IEnumerable<EditableModel<int>> result = input.ToEditableModels();

            CollectionAssert.AreEqual(input.ToList(), result.Select(em => em.Value).ToList());
        }

        [TestMethod]
        public void TestStaticValuesOnNull()
        {
            IEnumerable<EditableModel<object>> input = null;
            // ReSharper disable once ExpressionIsAlwaysNull
            IEnumerable<object> result = input.Values();

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestStaticValuesOnEmpty()
        {
            IEnumerable<EditableModel<object>> input = Enumerable.Empty<EditableModel<object>>();

            IEnumerable<object> result = input.Values();

            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public void TestStaticValuesOnArray()
        {
            var input = new[]
            {
                new EditableModel<int>(1),
                new EditableModel<int>(2),
                new EditableModel<int>(3)
            };

            IEnumerable<int> result = input.Values();

            CollectionAssert.AreEqual(result.ToList(), new[] { 1, 2, 3 });
        }
    }
}
