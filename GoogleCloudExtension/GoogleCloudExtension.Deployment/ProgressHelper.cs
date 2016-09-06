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
    internal static class ProgressHelper
    {
        internal static async Task<T> UpdateProgress<T>(
            Task<T> deployTask,
            IProgress<double> progress,
            double from, double to)
        {
            double current = from;
            while (true)
            {
                progress.Report(current);

                var resultTask = await Task.WhenAny(deployTask, Task.Delay(5000));
                if (resultTask == deployTask)
                {
                    return await deployTask;
                }

                current += 0.025;
                current = Math.Min(current, to);
            }
        }
    }
}
