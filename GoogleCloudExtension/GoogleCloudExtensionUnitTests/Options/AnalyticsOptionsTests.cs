using GoogleCloudExtension.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleCloudExtensionUnitTests.Options
{
    [TestClass]
    public class AnalyticsOptionsTests
    {
        [TestMethod]
        public void TestInitialConditions()
        {
            var objectUnderTest = new AnalyticsOptions();

            Assert.IsFalse(objectUnderTest.OptIn);
            Assert.IsFalse(objectUnderTest.DialogShown);
            Assert.IsNull(objectUnderTest.ClientId);
            Assert.IsNull(objectUnderTest.InstalledVersion);
        }

        [TestMethod]
        public void TestSetOptIn()
        {
            var objectUnderTest = new AnalyticsOptions();

            objectUnderTest.OptIn = true;

            Assert.IsTrue(objectUnderTest.OptIn);
        }

        [TestMethod]
        public void TestSetDialogShown()
        {
            var objectUnderTest = new AnalyticsOptions();

            objectUnderTest.DialogShown = true;

            Assert.IsTrue(objectUnderTest.DialogShown);
        }

        [TestMethod]
        public void TestSetClientId()
        {
            const string testClientId = "test-client-id-string";
            var objectUnderTest = new AnalyticsOptions();

            objectUnderTest.ClientId = testClientId;

            Assert.AreEqual(testClientId, objectUnderTest.ClientId);
        }

        [TestMethod]
        public void TestSetInstalledVersion()
        {
            const string testVersionString = "test-version-string";
            var objectUnderTest = new AnalyticsOptions();

            objectUnderTest.InstalledVersion = testVersionString;

            Assert.AreEqual(testVersionString, objectUnderTest.InstalledVersion);
        }

        [TestMethod]
        public void TestResetSettings()
        {
            const string testClientId = "test-client-id-string";
            var objectUnderTest = new AnalyticsOptions
            {
                ClientId = testClientId,
                DialogShown = true,
                OptIn = true
            };

            objectUnderTest.ResetSettings();

            Assert.IsFalse(objectUnderTest.OptIn);
            Assert.IsFalse(objectUnderTest.DialogShown);
            Assert.IsNull(objectUnderTest.ClientId);
        }

        [TestMethod]
        public void TestSaveSettingsSettingsInitalizesClientId()
        {
            var objectUnderTest = new AnalyticsOptions
            {
                OptIn = true,
                ClientId = null
            };

            objectUnderTest.SaveSettingsToStorage();

            Assert.IsNotNull(objectUnderTest.ClientId);
        }

        [TestMethod]
        public void TestSaveSettingsSettingsDiablesClientId()
        {
            var objectUnderTest = new AnalyticsOptions
            {
                ClientId = "test-client-id-string",
                OptIn = false
            };

            objectUnderTest.SaveSettingsToStorage();

            Assert.IsNull(objectUnderTest.ClientId);
        }
    }
}
