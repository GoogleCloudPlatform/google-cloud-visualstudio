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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources.UnitTests
{
    /// <summary>
    /// Test class for <seealso cref="OperationUtils"/>.
    /// </summary>
    [TestClass]
    public class OperationUtilsUnitTests
    {
        [TestMethod]
        public async Task TestCompletedOperation()
        {
            int count = 0;
            object operationPlaceholder = new object();

            await OperationUtils.AwaitOperationAsync<object, object>(
                operationPlaceholder,
                refreshOperation: x => { count++; return Task.FromResult(x); },
                isFinished: x => count >= 5,
                getErrorData: x => null,
                getErrorMessage: x => null,
                delay: TimeSpan.Zero);
        }

        [TestMethod]
        public async Task TestFailedOperation()
        {
            object operationPlaceholder = new object();
            bool getErrorDataCalled = false;
            bool getErrorMessageCalled = false;

            try
            {
                await OperationUtils.AwaitOperationAsync(
                    operationPlaceholder,
                    refreshOperation: x => Task.FromResult(x),
                    isFinished: _ => true,
                    getErrorData: x => { getErrorDataCalled = true; return x; },
                    getErrorMessage: x => { getErrorMessageCalled = true; return "Error message"; });

                // Should not reach here, an exception should be thrown.
                Assert.Fail();
            }
            catch (DataSourceException)
            {
                // We should throw DataSourceException, and the method to get the error data
                // should have been called.
                Assert.IsTrue(getErrorDataCalled && getErrorMessageCalled);
            }
        }

        [TestMethod]
        public async Task TestCancelledOperation()
        {
            bool exceptionThrown = false;
            try
            {
                object operationPlaceholder = new object();
                CancellationTokenSource source = new CancellationTokenSource();

                Task operationTask = OperationUtils.AwaitOperationAsync<object, object>(
                    operationPlaceholder,
                    refreshOperation: x => Task.FromResult(x),
                    isFinished: x => false,
                    getErrorData: x => null,
                    getErrorMessage: x => null,
                    token: source.Token);

                source.Cancel();
                await operationTask;
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TaskCanceledException)
            {
                exceptionThrown = true;
            }

            Assert.IsTrue(exceptionThrown);
        }
    }
}
