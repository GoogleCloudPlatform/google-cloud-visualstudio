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

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.UserPrompt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Project = Google.Apis.CloudResourceManager.v1.Data.Project;

namespace GoogleCloudExtensionUnitTests.Accounts
{
    /// <summary>
    /// Summary description for WindowsCredentialsStoreTest
    /// </summary>
    [TestClass]
    public class WindowsCredentialsStoreTest : ExtensionTestBase
    {
        private static readonly Instance s_defaultInstance = new Instance
        {
            Name = "DefaultInstanceName",
            Zone = "https://www.googleapis.com/compute/v1/projects/deploy-from-visual-studio-dev/zones/default-zone"
        };

        private WindowsCredentialsStore _objectUnderTest;
        private Mock<Func<string, bool>> _directoryExistsMock;
        private Mock<Func<string, IEnumerable<string>>> _enumerateFilesMock;
        private Mock<Func<string, byte[]>> _readAllBytesMock;
        private Mock<Action<string, byte[]>> _writeAllBytesMock;
        private Mock<Func<string, DirectoryInfo>> _createDirectoryMock;
        private Mock<Action<string>> _deleteFileMock;
        private Mock<Func<byte[], byte[], DataProtectionScope, byte[]>> _protectMock;
        private Mock<Func<byte[], byte[], DataProtectionScope, byte[]>> _unprotectMock;

        protected override void BeforeEach()
        {
            CredentialsStore.Default.UpdateCurrentProject(
                new Project { Name = "DefaultProjectName", ProjectId = "DefaultProjectId" });
            _directoryExistsMock = new Mock<Func<string, bool>>();
            _enumerateFilesMock = new Mock<Func<string, IEnumerable<string>>>();
            _readAllBytesMock = new Mock<Func<string, byte[]>>();
            _writeAllBytesMock = new Mock<Action<string, byte[]>>();
            _createDirectoryMock = new Mock<Func<string, DirectoryInfo>>();
            _deleteFileMock = new Mock<Action<string>>();

            _protectMock = new Mock<Func<byte[], byte[], DataProtectionScope, byte[]>>();
            _unprotectMock = new Mock<Func<byte[], byte[], DataProtectionScope, byte[]>>();

            _objectUnderTest = new WindowsCredentialsStore
            {
                DirectoryExists = _directoryExistsMock.Object,
                EnumerateFiles = _enumerateFilesMock.Object,
                ReadAllBytes = _readAllBytesMock.Object,
                WriteAllBytes = _writeAllBytesMock.Object,
                CreateDirectory = _createDirectoryMock.Object,
                DeleteFile = _deleteFileMock.Object,
                Protect = _protectMock.Object,
                Unprotect = _unprotectMock.Object
            };
        }

        [TestMethod]
        public void TestGetCredentialsForInstanceReturnsEmptyListForNonExistantDirectory()
        {
            _directoryExistsMock.Setup(f => f(It.IsAny<string>())).Returns(false);

            List<WindowsInstanceCredentials> result = _objectUnderTest.GetCredentialsForInstance(s_defaultInstance).ToList();

            CollectionAssert.AreEqual(new List<WindowsInstanceCredentials>(), result);
        }

        [TestMethod]
        public void TestGetCredentialsForInstanceSkipsFilesWithWrongExtension()
        {
            _directoryExistsMock.Setup(f => f(It.IsAny<string>())).Returns(true);
            _enumerateFilesMock.Setup(f => f(It.IsAny<string>())).Returns(new[] { @"c:\badExtension.bad" });

            List<WindowsInstanceCredentials> result =
                _objectUnderTest.GetCredentialsForInstance(s_defaultInstance).ToList();

            CollectionAssert.AreEqual(new List<WindowsInstanceCredentials>(), result);
        }

        [TestMethod]
        public void TestGetCredentialsForInstanceLoadsCredentialsFile()
        {
            const string credentialFilePath = @"c:\username.data";
            var encryptedBytes = new byte[] { 1, 2, 3 };
            const string passwordString = "passwordString";
            _directoryExistsMock.Setup(f => f(It.IsAny<string>())).Returns(true);
            _enumerateFilesMock.Setup(f => f(It.IsAny<string>())).Returns(new[] { credentialFilePath });
            _readAllBytesMock.Setup(f => f(credentialFilePath)).Returns(() => encryptedBytes);
            _unprotectMock.Setup(f => f(encryptedBytes, It.IsAny<byte[]>(), It.IsAny<DataProtectionScope>()))
                .Returns(() => Encoding.UTF8.GetBytes(passwordString));

            List<WindowsInstanceCredentials> result =
                _objectUnderTest.GetCredentialsForInstance(s_defaultInstance).ToList();

            CollectionAssert.AreEqual(
                new[] { new WindowsInstanceCredentials("username", passwordString) }, result);
        }

        [TestMethod]
        public void TestGetCredentialslForInstanceSortsByUsername()
        {
            var encryptedBytes = new byte[] { 1, 2, 3 };
            const string passwordString = "passwordString";
            _directoryExistsMock.Setup(f => f(It.IsAny<string>())).Returns(true);
            _enumerateFilesMock.Setup(f => f(It.IsAny<string>())).Returns(new[] { @"c:\b.data", @"c:\c.data", @"c:\a.data" });
            _readAllBytesMock.Setup(f => f(It.IsAny<string>())).Returns(() => encryptedBytes);
            _unprotectMock.Setup(f => f(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<DataProtectionScope>()))
                .Returns(() => Encoding.UTF8.GetBytes(passwordString));

            List<WindowsInstanceCredentials> result =
                _objectUnderTest.GetCredentialsForInstance(s_defaultInstance).ToList();

            var expected = new[]
            {
                new WindowsInstanceCredentials("a", passwordString),
                new WindowsInstanceCredentials("b", passwordString),
                new WindowsInstanceCredentials("c", passwordString)
            };
            CollectionAssert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestGetCredentialsForInstanceHandlesReadIoException()
        {
            const string passwordString = "passwordString";
            _directoryExistsMock.Setup(f => f(It.IsAny<string>())).Returns(true);
            _enumerateFilesMock.Setup(f => f(It.IsAny<string>())).Returns(new[] { @"c:\username.data" });
            _readAllBytesMock.Setup(f => f(It.IsAny<string>())).Throws(new IOException("Test Exception Message"));
            _unprotectMock.Setup(f => f(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<DataProtectionScope>()))
                .Returns(() => Encoding.UTF8.GetBytes(passwordString));

            List<WindowsInstanceCredentials> result =
                _objectUnderTest.GetCredentialsForInstance(s_defaultInstance).ToList();

            var expected = new WindowsInstanceCredentials[] { };
            CollectionAssert.AreEqual(expected, result);
            PromptUserMock.Verify(
                f => f(
                    It.Is<UserPromptWindow.Options>(
                        o => o.Title == Resources.WindowsCredentialsStoreCredentialFileLoadErrorTitle)),
                Times.Once);
        }

        [TestMethod]
        public void TestGetCredentialsForInstanceHandlesDecryptionException()
        {
            const string corruptedFilePath = @"c:\corrupted.data";
            _directoryExistsMock.Setup(f => f(It.IsAny<string>())).Returns(true);
            _enumerateFilesMock.Setup(f => f(It.IsAny<string>())).Returns(new[] { corruptedFilePath });
            _readAllBytesMock.Setup(f => f(It.IsAny<string>())).Returns(new byte[] { 1, 2, 3 });
            _unprotectMock.Setup(f => f(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<DataProtectionScope>()))
                .Throws(new CryptographicException("Test Exception Message"));

            List<WindowsInstanceCredentials> result =
                _objectUnderTest.GetCredentialsForInstance(s_defaultInstance).ToList();

            var expected = new WindowsInstanceCredentials[] { };
            CollectionAssert.AreEqual(expected, result);
            PromptUserMock.Verify(
                f => f(
                    It.Is<UserPromptWindow.Options>(
                        o => o.Title == Resources.WindowsCredentialsStoreDecryptionErrorTitle)),
                Times.Once);
            _deleteFileMock.Verify(f => f(corruptedFilePath), Times.Once);
        }

        [TestMethod]
        public void TestGetCredentialsForInstancePromptsOnDeleteException()
        {
            const string corruptedFilePath = @"c:\corrupted.data";
            _directoryExistsMock.Setup(f => f(It.IsAny<string>())).Returns(true);
            _enumerateFilesMock.Setup(f => f(It.IsAny<string>())).Returns(new[] { corruptedFilePath });
            _readAllBytesMock.Setup(f => f(It.IsAny<string>())).Returns(new byte[] { 1, 2, 3 });
            _unprotectMock.Setup(f => f(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<DataProtectionScope>()))
                .Throws(new CryptographicException("Test Exception Message"));
            _deleteFileMock.Setup(f => f(corruptedFilePath)).Throws(new IOException("Test Exception Message"));

            List<WindowsInstanceCredentials> result =
                _objectUnderTest.GetCredentialsForInstance(s_defaultInstance).ToList();

            CollectionAssert.AreEqual(new WindowsInstanceCredentials[] { }, result);
            PromptUserMock.Verify(
                f => f(
                    It.Is<UserPromptWindow.Options>(
                        o => o.Title == Resources.WindowsCredentialsStoreDeletingCorruptedErrorTitle)),
                Times.Once);
        }

        [TestMethod]
        public void TestAddCredentialsToInstanceCreatesMissingDirectory()
        {
            CredentialsStore.Default.UpdateCurrentProject(new Project { ProjectId = "TestProject" });
            _directoryExistsMock.Setup(f => f(It.IsAny<string>())).Returns(false);

            var instance = new Instance
            {
                Name = "TestInstance",
                Zone = "https://www.googleapis.com/compute/v1/projects/deploy-from-visual-studio-dev/zones/testzone"
            };
            _objectUnderTest.AddCredentialsToInstance(instance, new WindowsInstanceCredentials("user", "password"));

            string expectedPath = Path.Combine(
                WindowsCredentialsStore.s_credentialsStoreRoot, @"TestProject\testzone\TestInstance");
            _createDirectoryMock.Verify(f => f(expectedPath), Times.Once);
        }

        [TestMethod]
        public void TestAddCredentialsToInstanceWritesEncryptedPasswordToFile()
        {
            const string password = "testPassword";
            const string user = "testUser";
            byte[] encryptedBytes = { 1, 2, 3 };
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            _protectMock.Setup(f => f(passwordBytes, It.IsAny<byte[]>(), It.IsAny<DataProtectionScope>())).Returns(encryptedBytes);

            _objectUnderTest.AddCredentialsToInstance(s_defaultInstance, new WindowsInstanceCredentials(user, password));

            _writeAllBytesMock.Verify(
                f => f(
                    It.Is<string>(
                        s => s.EndsWith(
                            user + WindowsCredentialsStore.PasswordFileExtension, StringComparison.Ordinal)),
                    encryptedBytes),
                Times.Once);
        }
    }
}
