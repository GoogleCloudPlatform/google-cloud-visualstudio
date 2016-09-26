// Copyright 2016 Google Inc. All Rights Reserved.
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
using System.Threading.Tasks;

namespace GoogleCloudExtension.Deployment
{
    /// <summary>
    /// Utilities for dealing with <seealso cref="IProgress{T}"/> instances.
    /// </summary>
    internal static class ProgressHelper
    {
        private const int DefaultTaskWaitMilliseconds = 5000;
        private const double DefaultProgressIncrement = 0.025;

        /// <summary>
        /// Waits for a long running task, periodically updating the progress indicator.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="deployTask">The task to wait.</param>
        /// <param name="progress">The progress indicator to update.</param>
        /// <param name="from">The initial value.</param>
        /// <param name="to">The final value.</param>
        /// <returns></returns>
        internal static async Task<T> UpdateProgress<T>(
            Task<T> deployTask,
            IProgress<double> progress,
            double from, double to)
        {
            double current = from;
            while (true)
            {
                progress.Report(current);

                var resultTask = await Task.WhenAny(deployTask, Task.Delay(DefaultTaskWaitMilliseconds));
                if (resultTask == deployTask)
                {
                    return await deployTask;
                }

                current += DefaultProgressIncrement;
                current = Math.Min(current, to);
            }
        }
    }
}
