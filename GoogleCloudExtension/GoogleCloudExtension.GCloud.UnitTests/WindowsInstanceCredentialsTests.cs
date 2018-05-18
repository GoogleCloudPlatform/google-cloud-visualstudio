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
using System.Diagnostics.CodeAnalysis;

namespace GoogleCloudExtension.GCloud.UnitTests
{
    [TestClass]
    public class WindowsInstanceCredentialsTests
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private static object[][] NotEqualObjects { get; } =
        {
            new object[] {null},
            new object[] {"UserNameAndPassword"},
            new object[] {new WindowsInstanceCredentials("DifferentUser", "Password")},
            new object[] {new WindowsInstanceCredentials("User", "DifferentPassword")}
        };

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
            var json = @"{""username"":""UserString"",""password"":""PasswordString""}";

            var result = JsonConvert.DeserializeObject<WindowsInstanceCredentials>(json);

            Assert.AreEqual("UserString", result.User);
            Assert.AreEqual("PasswordString", result.Password);
        }

        [TestMethod]
        [DynamicData(nameof(NotEqualObjects))]
        public void TestEqualsFalse(object notEqualObject)
        {
            var sourceObject = new WindowsInstanceCredentials("User", "Password");
            Assert.IsFalse(sourceObject.Equals(notEqualObject));
        }

        [TestMethod]
        [DynamicData(nameof(NotEqualObjects))]
        public void TestHashCodeNotEqual(object notEqualObject)
        {
            var sourceObject = new WindowsInstanceCredentials("User", "Password");
            Assert.AreNotEqual(sourceObject.GetHashCode(), notEqualObject?.GetHashCode());
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "EqualExpressionComparison")]
        public void TestEqualsTrue()
        {
            const string user = "User";
            const string password = "Password";
            var source = new WindowsInstanceCredentials(user, password);
            var target = new WindowsInstanceCredentials(user, password);
            Assert.IsTrue(source.Equals(source));
            Assert.IsTrue(source.Equals(target));
        }

        [TestMethod]
        public void TestGetHashCodeEqual()
        {
            const string user = "User";
            const string password = "Password";
            Assert.AreEqual(
                new WindowsInstanceCredentials(user, password).GetHashCode(),
                new WindowsInstanceCredentials(user, password).GetHashCode());
        }
    }
}
