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

using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class contains the output of a running process.
    /// </summary>
    public sealed class ProcessOutput
    {
        /// <summary>
        /// Whether the process succeeded or not.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// The complete contents of the stderr stream.
        /// </summary>
        public string StandardError { get; }

        /// <summary>
        /// The complete contents of the stdout stream.
        /// </summary>
        public string StandardOutput { get; }

        public ProcessOutput(bool succeeded, string standardOutput, string standardError)
        {
            Succeeded = succeeded;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }
    }

    /// <summary>
    /// This class defines helper methods for starting sub-processes and getting the output from
    /// the processes, including a helper to parse the output as json.
    /// </summary>
    [Export(typeof(IProcessService))]
    public class ProcessUtils : IProcessService
    {
        /// <summary>
        /// The default <see cref="IProcessService"/>.
        /// </summary>
        public static IProcessService Default => GoogleCloudExtensionPackage.Instance.ProcessService;

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
        public Task<bool> RunCommandAsync(
            string file,
            string args,
            Action<string> handler,
            string workingDir = null,
            IDictionary<string, string> environment = null)
            => RunCommandAsync(
                file,
                args,
                s =>
                {
                    handler(s);
                    return Task.CompletedTask;
                },
                workingDir,
                environment);

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
        public async Task<bool> RunCommandAsync(
            string file,
            string args,
            Func<string, Task> handler,
            string workingDir = null,
            IDictionary<string, string> environment = null)
        {
            await TaskScheduler.Default;
            var process = new Process
            {
                StartInfo = GetStartInfoForInteractiveProcess(file, args, workingDir, environment),
                EnableRaisingEvents = true
            };

            Task executeTask = process.ExecuteAsync();
            var readErrorsTask = ReadLinesFromOutputAsync(process.StandardError, handler);
            var readOutputTask = ReadLinesFromOutputAsync(process.StandardOutput, handler);
            await Task.WhenAll(readErrorsTask, readOutputTask, executeTask);
            await executeTask;
            return process.ExitCode == 0;
        }

        /// <summary>
        /// Runs a process until it exists, returns it's complete output.
        /// </summary>
        /// <param name="file">The path to the executable.</param>
        /// <param name="args">The arguments to pass to the executable.</param>
        /// <param name="workingDir">The working directory to use, optional.</param>
        /// <param name="environment">The environment variables to use for the executable.</param>
        public Task<ProcessOutput> GetCommandOutputAsync(
            string file,
            string args,
            string workingDir = null,
            IDictionary<string, string> environment = null)
        {
            ProcessStartInfo startInfo = GetStartInfoForInteractiveProcess(
                file: file,
                args: args,
                workingDir: workingDir,
                environment: environment);

            return Task.Run(async () =>
            {
                Process process = Process.Start(startInfo);
                Debug.Assert(process != null, $"{nameof(process)} should never be null when UseShellExecute is false.");
                Task<string> readErrorsTask = process.StandardError.ReadToEndAsync();
                Task<string> readOutputTask = process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();
                bool succeeded = process.ExitCode == 0;
                return new ProcessOutput(
                    succeeded: succeeded,
                    standardOutput: await readOutputTask,
                    standardError: await readErrorsTask);
            });
        }

        /// <summary>
        /// Launches a process and parses its stdout stream as a json value to an instance of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to use to deserialize the stdout stream.</typeparam>
        /// <param name="file">The path to the executable.</param>
        /// <param name="args">The arguments to pass to the executable.</param>
        /// <param name="workingDir">The working directory to use, optional.</param>
        /// <param name="environment">The environment to use for the executable.</param>
        public async Task<T> GetJsonOutputAsync<T>(
            string file,
            string args,
            string workingDir = null,
            IDictionary<string, string> environment = null)
        {
            ProcessOutput output = await GetCommandOutputAsync(
                file: file,
                args: args,
                workingDir: workingDir,
                environment: environment);
            if (!output.Succeeded)
            {
                throw new JsonOutputException($"Failed to execute command: {file} {args}\n{output.StandardError}");
            }
            try
            {
                return JsonConvert.DeserializeObject<T>(output.StandardOutput);
            }
            catch (JsonException ex)
            {
                throw new JsonOutputException($"Failed to parse output of command: {file} {args}\n{output.StandardOutput}", ex);
            }
        }

        private static ProcessStartInfo GetStartInfoForInteractiveProcess(
            string file,
            string args,
            string workingDir,
            IDictionary<string, string> environment)
        {
            // If the caller provides a working directory use it otherwise default to the user's profile directory
            // so we have a stable working directory instead of a random working directory as Visual Studio changes the
            // current working directory often.
            var startInfo = new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                WorkingDirectory = workingDir ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true
            };

            // Customize the environment for the incoming process.
            if (environment != null)
            {
                foreach (KeyValuePair<string, string> entry in environment)
                {
                    startInfo.EnvironmentVariables[entry.Key] = entry.Value;
                }
            }
            return startInfo;
        }

        private static async Task ReadLinesFromOutputAsync(
            TextReader stream,
            Func<string, Task> handler)
        {
            if (handler != null)
            {
                string line;
                while ((line = await stream.ReadLineAsync()) != null)
                {
                    await handler(line);
                }
            }
        }
    }
}
