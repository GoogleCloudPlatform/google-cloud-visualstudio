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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Service interface for running <see cref="System.Diagnostics.Process"/>.
    /// </summary>
    public interface IProcessService
    {
        /// <summary>
        /// Launches a process and parses its stdout stream as a json value to an instance of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to use to deserialize the stdout stream.</typeparam>
        /// <param name="file">The path to the executable.</param>
        /// <param name="args">The arguments to pass to the executable.</param>
        /// <param name="workingDir">The working directory to use, optional.</param>
        /// <param name="environment">The environment to use for the executable.</param>
        Task<T> GetJsonOutputAsync<T>(
            string file,
            string args,
            string workingDir = null,
            IDictionary<string, string> environment = null);

        /// <summary>
        /// Runs the given binary given by <paramref name="file"/> with the passed in <paramref name="args"/> and
        /// reads the output of the new process as it happens, calling <paramref name="handler"/> with each line being output
        /// by the process.
        /// Uses <paramref name="environment"/> if provided to customize the environment of the child process.
        /// </summary>
        /// <param name="file">The path to the binary to execute, it must not be null.</param>
        /// <param name="args">The arguments to pass to the binary to execute, it can be null.</param>
        /// <param name="handler">The callback to call with the line being output by the process, it can be called outside
        /// of the UI thread. Must not be null.</param>
        /// <param name="workingDir">The working directory to use, optional.</param>
        /// <param name="environment">Optional parameter with values for environment variables to pass on to the child process.</param>
        Task<bool> RunCommandAsync(
            string file,
            string args,
            Func<string, Task> handler,
            string workingDir = null,
            IDictionary<string, string> environment = null);

        /// <summary>
        /// Runs the given binary given by <paramref name="file"/> with the passed in <paramref name="args"/> and
        /// reads the output of the new process as it happens, calling <paramref name="handler"/> with each line being output
        /// by the process.
        /// Uses <paramref name="environment"/> if provided to customize the environment of the child process.
        /// </summary>
        /// <param name="file">The path to the binary to execute, it must not be null.</param>
        /// <param name="args">The arguments to pass to the binary to execute, it can be null.</param>
        /// <param name="handler">The callback to call with the line being output by the process, it can be called outside
        /// of the UI thread. Must not be null.</param>
        /// <param name="workingDir">The working directory to use, optional.</param>
        /// <param name="environment">Optional parameter with values for environment variables to pass on to the child process.</param>
        Task<bool> RunCommandAsync(
            string file,
            string args,
            Action<string> handler,
            string workingDir = null,
            IDictionary<string, string> environment = null);
    }
}