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

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using GoogleCloudExtension.Utils;
using Moq;
using Moq.Language.Flow;

namespace GoogleCloudExtensionUnitTests.GCloud
{
    public static class ProcessServiceMockExtensions
    {
        public static ISetup<IProcessService, Task<bool>> SetupRunCommandAsync(this Mock<IProcessService> processServiceMock)
        {
            return processServiceMock.Setup(
                p => p.RunCommandAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Func<string, Task>>(),
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, string>>()));
        }

        public static ISetup<IProcessService, Task<T>> SetupGetJsonOutput<T>(
            this Mock<IProcessService> processServiceMock)
        {
            return processServiceMock.Setup(
                p => p.GetJsonOutputAsync<T>(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()));
        }

        public static void VerifyRunCommandAsyncEnvironment(
            this Mock<IProcessService> processServiceMock,
            Expression<Func<IDictionary<string, string>, bool>> match)
        {
            processServiceMock.Verify(
                p => p.RunCommandAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Func<string, Task>>(),
                    It.IsAny<string>(),
                    It.Is(match)));
        }

        public static void VerifyRunCommandAsyncArgs(
            this Mock<IProcessService> processServiceMock,
            Expression<Func<string, bool>> match)
        {
            processServiceMock.Verify(
                p => p.RunCommandAsync(
                    It.IsAny<string>(),
                    It.Is(match),
                    It.IsAny<Func<string, Task>>(),
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, string>>()));
        }

        public static void VerifyRunCommandAsyncArgs(
            this Mock<IProcessService> processServiceMock,
            string command,
            Expression<Func<string, bool>> match)
        {
            processServiceMock.Verify(
                p => p.RunCommandAsync(
                    command,
                    It.Is(match),
                    It.IsAny<Func<string, Task>>(),
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, string>>()));
        }

        public static void VerifyRunCommandAsyncHandler(
            this Mock<IProcessService> processServiceMock,
            Func<string, Task> handler)
        {
            processServiceMock.Verify(
                p => p.RunCommandAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    handler,
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, string>>()));
        }

        public static void VerifyGetJsonOutputAsyncArgs<T>(
            this Mock<IProcessService> processServiceMock,
            Expression<Func<string, bool>> match)
        {
            processServiceMock.Verify(
                p => p.GetJsonOutputAsync<T>(
                    It.IsAny<string>(),
                    It.Is(match),
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, string>>()));
        }

        public static void VerifyGetJsonOutputAsyncArgs<T>(
            this Mock<IProcessService> processServiceMock,
            string command,
            Expression<Func<string, bool>> match)
        {
            processServiceMock.Verify(
                p => p.GetJsonOutputAsync<T>(
                    command,
                    It.Is(match),
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, string>>()));
        }
    }
}