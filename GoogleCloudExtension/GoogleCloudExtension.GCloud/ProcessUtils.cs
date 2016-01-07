// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GCloud
{
    internal class ProcessOutput
    {
        public ProcessOutput(bool succeeded, string output, string error)
        {
            this.Succeeded = succeeded;
            this.Output = output;
            this.Error = error;
        }

        public bool Succeeded { get; private set; }
        public string Error { get; private set; }
        public string Output { get; private set; }
    }

    internal enum OutputChannel
    {
        None,
        StdError,
        StdOutput
    }

    internal class OutputHandlerEventArgs
    {
        public string Line { get; set; }
        public OutputChannel Channel { get; set; }
    }

    internal delegate void OutputHandler(object sender, OutputHandlerEventArgs args);

    internal static class ProcessUtils
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
        public static async Task<bool> RunCommandAsync(string file, string args, OutputHandler handler, IDictionary<string, string> environment)
        {
            var startInfo = GetStartInfoForInteractiveProcess(file, args, environment);

            return await Task.Run(async () =>
            {
                var process = Process.Start(startInfo);
                var readErrorsTask = ReadLinesFromOutput(OutputChannel.StdError, process.StandardError, handler);
                var readOutputTask = ReadLinesFromOutput(OutputChannel.StdOutput, process.StandardOutput, handler);
                await readErrorsTask;
                await readOutputTask;
                process.WaitForExit();
                return process.ExitCode == 0;
            });
        }

        public static async Task<ProcessOutput> GetCommandOutputAsync(string file, string args, IDictionary<string, string> environment)
        {
            var startInfo = GetStartInfoForInteractiveProcess(file, args, environment);

            return await Task.Run(async () =>
            {
                var process = Process.Start(startInfo);
                var readErrorsTask = process.StandardError.ReadToEndAsync();
                var readOutputTask = process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();
                var succeeded = process.ExitCode == 0;
                return new ProcessOutput(
                    succeeded: succeeded,
                    output: await readOutputTask,
                    error: await readErrorsTask);
            });
        }

        /// <summary>
        /// Launches a process with the given parameters and environment, does not parse the output.
        /// </summary>
        /// <param name="file">The file with the process to execute.</param>
        /// <param name="args">The arguments for the process.</param>
        /// <param name="environment">The environment to use for executing the proces.</param>
        /// <returns>A task that will resolve to the exit code of the process.</returns>
        public static async Task<int> LaunchCommandAsync(string file, string args, IDictionary<string, string> environment)
        {
            var startInfo = GetBaseStartInfo(file, args, environment);
            return await Task.Run(() =>
            {
                var process = Process.Start(startInfo);
                process.WaitForExit();
                return process.ExitCode;
            });
        }

        public static async Task<T> GetJsonOutputAsync<T>(string file, string args, IDictionary<string, string> environment)
        {
            var output = await ProcessUtils.GetCommandOutputAsync(file, args, environment);
            if (!output.Succeeded)
            {
                throw new JsonOutputException($"Failed to execute command: {file} {args}\n{output.Error}");
            }
            try
            {
                var parsed = JsonConvert.DeserializeObject<T>(output.Output);
                return ValueOrDefault(parsed);
            }
            catch (JsonSerializationException)
            {
                throw new JsonOutputException($"Failed to parse output of command: {file} {args}\n{output.Output}");
            }
        }

        /// <summary>
        /// This method is justy a passthrough, specifically designed for non-list parameters.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="src">The value to transform</param>
        /// <returns></returns>
        private static T ValueOrDefault<T>(T src)
        {
            return src;
        }

        /// <summary>
        /// This method will transform a null list into an empty list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="src">The source list.</param>
        /// <returns></returns>
        private static IList<T> ValueOrDefault<T>(IList<T> src)
        {
            if (src == null)
            {
                return new List<T>();
            }
            return src;
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

        private static async Task ReadLinesFromOutput(OutputChannel channel, StreamReader stream, OutputHandler handler)
        {
            await Task.Run(() =>
            {
                while (!stream.EndOfStream)
                {
                    var line = stream.ReadLine();
                    handler(null, new OutputHandlerEventArgs { Channel = channel, Line = line });
                }
            });
        }
    }
}
