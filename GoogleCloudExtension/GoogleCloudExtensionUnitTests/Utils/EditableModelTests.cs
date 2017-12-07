using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.ComponentModel;

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
    }
}
