﻿// Copyright 2016 Google Inc. All Rights Reserved.
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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
    /// The output streams from a process.
    /// </summary>
    public enum OutputStream
    {
        None,
        StandardError,
        StandardOutput
    }

    /// <summary>
    /// Evemt args passed to the output handler of a process.
    /// </summary>
    public class OutputHandlerEventArgs : EventArgs
    {
        public string Line { get; }
        public OutputStream OutputStream { get; }

        public OutputHandlerEventArgs(string line, OutputStream stream)
        {
            Line = line;
            OutputStream = stream;
        }
    }

    /// <summary>
    /// This class defines helper methods for starting sub-processes and getting the output from
    /// the processes, including a helper to parse the output as json.
    /// </summary>
    public static class ProcessUtils
    {
        /// <summary>
        /// Runs the given binary given by <paramref name="file"/> with the passed in <paramref name="args"/> and
        /// reads the output of the new process as it happens, calling <paramref name="handler"/> with each line being output
        /// by the process.
        /// Uses <paramref name="environment"/> if provided to customize the environment of the child process.
        /// </summary>
        /// <param name="file">The path to the binary to execute, it should not be null.</param>
        /// <param name="args">The arguments to pass to the binary to execute, it can be null.</param>
        /// <param name="handler">The callback to call with wach line being oput by the process, it can be called outside
        /// of the UI thread. Must not be null.</param>
        /// <param name="environment">Optional parameter with values for environment variables to pass on to the child process.</param>
        /// <returns></returns>
        public static Task<bool> RunCommandAsync(
            string file,
            string args,
            EventHandler<OutputHandlerEventArgs> handler,
            IDictionary<string, string> environment)
        {
            var startInfo = GetStartInfoForInteractiveProcess(file, args, environment);

            return Task.Run(async () =>
            {
                var process = Process.Start(startInfo);
                var readErrorsTask = ReadLinesFromOutput(OutputStream.StandardError, process.StandardError, handler);
                var readOutputTask = ReadLinesFromOutput(OutputStream.StandardOutput, process.StandardOutput, handler);
                await readErrorsTask;
                await readOutputTask;
                process.WaitForExit();
                return process.ExitCode == 0;
            });
        }

        /// <summary>
        /// Runs a process until it exists, returns it's complete output.
        /// </summary>
        /// <param name="file">The path to the exectuable.</param>
        /// <param name="args">The arguments to pass to the executable.</param>
        /// <param name="environment">The environment variables to use for the executable.</param>
        /// <returns></returns>
        public static Task<ProcessOutput> GetCommandOutputAsync(string file, string args, IDictionary<string, string> environment)
        {
            var startInfo = GetStartInfoForInteractiveProcess(file, args, environment);

            return Task.Run(async () =>
            {
                var process = Process.Start(startInfo);
                var readErrorsTask = process.StandardError.ReadToEndAsync();
                var readOutputTask = process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();
                var succeeded = process.ExitCode == 0;
                return new ProcessOutput(
                    succeeded: succeeded,
                    standardOutput: await readOutputTask,
                    standardError: await readErrorsTask);
            });
        }

        /// <summary>
        /// Launches a process with the given parameters and environment, does not parse the output.
        /// </summary>
        /// <param name="file">The file with the process to execute.</param>
        /// <param name="args">The arguments for the process.</param>
        /// <param name="environment">The environment to use for executing the proces.</param>
        /// <returns>A task that will resolve to the exit code of the process.</returns>
        public static Task<int> LaunchCommandAsync(string file, string args, IDictionary<string, string> environment)
        {
            var startInfo = GetBaseStartInfo(file, args, environment);
            return Task.Run(() =>
            {
                var process = Process.Start(startInfo);
                process.WaitForExit();
                return process.ExitCode;
            });
        }

        /// <summary>
        /// Launches a process and parses its stdout stream as a json value to an instance of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to use to deserialize the stdout stream.</typeparam>
        /// <param name="file">The path to the exectuable.</param>
        /// <param name="args">The arguments to pass to the executable.</param>
        /// <param name="environment">The environment to use for the executable.</param>
        /// <returns></returns>
        public static async Task<T> GetJsonOutputAsync<T>(string file, string args, IDictionary<string, string> environment)
        {
            var output = await ProcessUtils.GetCommandOutputAsync(file, args, environment);
            if (!output.Succeeded)
            {
                throw new JsonOutputException($"Failed to execute command: {file} {args}\n{output.StandardError}");
            }
            try
            {
                return JsonConvert.DeserializeObject<T>(output.StandardOutput);
            }
            catch (JsonException)
            {
                throw new JsonOutputException($"Failed to parse output of command: {file} {args}\n{output.StandardOutput}");
            }
        }

        private static ProcessStartInfo GetBaseStartInfo(string file, string args, IDictionary<string, string> environment)
        {
            // Always start the tool in the user's home directory, avoid random directories
            // coming from Visual Studio.
            ProcessStartInfo result = new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                WorkingDirectory = Environment.GetEnvironmentVariable("USERPROFILE"),
            };

            // Customize the environment for the incoming process.
            if (environment != null)
            {
                foreach (var entry in environment)
                {
                    result.EnvironmentVariables[entry.Key] = entry.Value;
                }
            }

            return result;
        }

        private static ProcessStartInfo GetStartInfoForInteractiveProcess(string file, string args, IDictionary<string, string> environment)
        {
            ProcessStartInfo startInfo = GetBaseStartInfo(file, args, environment);
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardInput = true;
            return startInfo;
        }

        private static Task ReadLinesFromOutput(OutputStream outputStream, StreamReader stream, EventHandler<OutputHandlerEventArgs> handler)
        {
            return Task.Run(() =>
            {
                while (!stream.EndOfStream)
                {
                    var line = stream.ReadLine();
                    handler(null, new OutputHandlerEventArgs(line, outputStream));
                }
            });
        }
    }
}
