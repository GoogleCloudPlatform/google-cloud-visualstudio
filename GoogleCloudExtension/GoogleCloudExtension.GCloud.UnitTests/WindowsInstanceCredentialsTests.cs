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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;

namespace GoogleCloudExtension.GCloud.UnitTests
{
    [TestClass]
    public class WindowsInstanceCredentialsTests
    {
        private const string DefaultPassword = "DefaultPassword";
        private const string DefaultUser = "DefaultUser";

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private static object[][] NotEqualObjects { get; } =
        {
            new object[] {null},
            new object[] {"UserNameAndPassword"},
            new object[] {new WindowsInstanceCredentials("DifferentUser", DefaultPassword)},
            new object[] {new WindowsInstanceCredentials(DefaultUser, "DifferentPassword")},
            new object[] {new WindowsInstanceCredentials(DefaultUser, null)}
        };

        [TestMethod]
        public void TestConstructor_ThrowsFOrNullUser()
        {
            var exception = Assert.ThrowsException<ArgumentNullException>(
                () => new WindowsInstanceCredentials(null, DefaultPassword));

            Assert.AreEqual("user", exception.ParamName);
        }

        [TestMethod]
        public void TestConstructor_SetUser()
        {
            const string exptectedUser = "ExpectedUser";
            var objectUnderTest = new WindowsInstanceCredentials(exptectedUser, DefaultPassword);

            Assert.AreEqual(exptectedUser, objectUnderTest.User);
        }

        [TestMethod]
        public void TestConstructor_SetPassword()
        {
            const string exptectedPassword = "ExpectedPassword";
            var objectUnderTest = new WindowsInstanceCredentials(DefaultUser, exptectedPassword);

            Assert.AreEqual(exptectedPassword, objectUnderTest.Password);
        }

        [TestMethod]
        public void TestConstructor_AcceptsNullPassword()
        {
            var objectUnderTest = new WindowsInstanceCredentials(DefaultUser, null);

            Assert.IsNull(objectUnderTest.Password);
        }

        [TestMethod]
        public void TestJsonSerialization()
        {
            var objectUnderTest = new WindowsInstanceCredentials("UserString", "PasswordString");

            string json = JsonConvert.SerializeObject(objectUnderTest);

            Assert.AreEqual(@"{""username"":""UserString"",""password"":""PasswordString""}", json);
        }

        [TestMethod]
        public void TestJsonDeserialization()
        {
            const string json = @"{""username"":""UserString"",""password"":""PasswordString""}";

            var result = JsonConvert.DeserializeObject<WindowsInstanceCredentials>(json);

            Assert.AreEqual("UserString", result.User);
            Assert.AreEqual("PasswordString", result.Password);
        }

        [TestMethod]
        [DynamicData(nameof(NotEqualObjects))]
        public void TestEqualsFalse(object notEqualObject)
        {
            var sourceObject = new WindowsInstanceCredentials(DefaultUser, DefaultPassword);
            Assert.IsFalse(sourceObject.Equals(notEqualObject));
        }

        [TestMethod]
        [DynamicData(nameof(NotEqualObjects))]
        public void TestHashCodeNotEqual(object notEqualObject)
        {
            var sourceObject = new WindowsInstanceCredentials(DefaultUser, DefaultPassword);
            Assert.AreNotEqual(sourceObject.GetHashCode(), notEqualObject?.GetHashCode());
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "EqualExpressionComparison")]
        public void TestEqualsTrue()
        {
            const string user = DefaultUser;
            const string password = DefaultPassword;
            var source = new WindowsInstanceCredentials(user, password);
            var target = new WindowsInstanceCredentials(user, password);
            Assert.IsTrue(source.Equals(source));
            Assert.IsTrue(source.Equals(target));
        }

        [TestMethod]
        public void TestGetHashCodeEqual()
        {
            const string user = DefaultUser;
            const string password = DefaultPassword;
            Assert.AreEqual(
                new WindowsInstanceCredentials(user, password).GetHashCode(),
                new WindowsInstanceCredentials(user, password).GetHashCode());
        }
    }
}
