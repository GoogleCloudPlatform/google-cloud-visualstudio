// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;
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
        public static async Task<bool> RunCommandAsync(string file, string args, OutputHandler handler, Dictionary<string, string> environment)
        {
            var startInfo = GetStartInfo(file, args, environment);

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

        public static async Task<ProcessOutput> GetCommandOutputAsync(string file, string args, Dictionary<string, string> environment)
        {
            var startInfo = GetStartInfo(file, args, environment);

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

        public static async Task<T> GetJsonOutputAsync<T>(string file, string args, Dictionary<string, string> environment)
        {
            var output = await ProcessUtils.GetCommandOutputAsync(file, args, environment);
            if (!output.Succeeded)
            {
                throw new JsonOutputException($"Failed to execute command: {file} {args}\n{output.Error}");
            }
            var parsed = JsonConvert.DeserializeObject<T>(output.Output);
            return parsed;
        }

        private static ProcessStartInfo GetStartInfo(string file, string args, Dictionary<string, string> environment)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                FileName = file,
                Arguments = args,
            };

            // Customize the environment for the incoming process.
            if (environment != null)
            {
                foreach (var entry in environment)
                {
                    startInfo.EnvironmentVariables[entry.Key] = entry.Value;
                }
            }

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
