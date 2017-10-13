using Google;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    internal static class Operations
    {
        private static readonly TimeSpan s_defaultDelay = TimeSpan.FromMilliseconds(2000);

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
                    var newOperation = await refreshOperation(operation);

                    if (isFinished(newOperation))
                    {
                        string errorData = getErrorData(newOperation);
                        if (errorData != null)
                        {
                            throw new DataSourceException($"Operation failed: {errorData}.");
                        }
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
