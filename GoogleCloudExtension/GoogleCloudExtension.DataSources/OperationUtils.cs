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

using Google;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// This class contains utility methods to apply to Operations created from data sources.
    /// </summary>
    public static class OperationUtils
    {
        // A default delay of 2 seconds in between polls.
        private static readonly TimeSpan s_defaultDelay = TimeSpan.FromMilliseconds(2000);

        /// <summary>
        /// This method will poll the state of an operation, effectively transforming an operation into a <seealso cref="Task"/>.
        /// </summary>
        /// <typeparam name="TOperation">The type of the operation to poll.</typeparam>
        /// <param name="operation">The operation to poll.</param>
        /// <param name="refreshOperation">An function that will refresh the operation, returning an updated instance.</param>
        /// <param name="isFinished">A function thgat determines if the operation is done.</param>
        /// <param name="getErrorData">A function that will extract the error message from the operation, if in error.</param>
        /// <param name="delay">The delay to use in between refreshes.</param>
        /// <returns>A <seealso cref="Task"/> that will be completed once the operation is done.</returns>
        public static async Task AwaitOperationAsync<TOperation>(
            TOperation operation,
            Func<TOperation, Task<TOperation>> refreshOperation,
            Func<TOperation, bool> isFinished,
            Func<TOperation, string> getErrorData,
            TimeSpan? delay = null)
        {
            try
            {
                while (true)
                {
                    Debug.WriteLine("Polling for operation to finish.");
                    var newOperation = await refreshOperation(operation);

                    if (isFinished(newOperation))
                    {
                        Debug.WriteLine("Operation finished.");
                        string errorData = getErrorData(newOperation);
                        if (errorData != null)
                        {
                            throw new DataSourceException($"Operation failed: {errorData}.");
                        }
                        return;
                    }

                    await Task.Delay(delay ?? s_defaultDelay);
                }
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to read operation: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }

        }

    }
}
