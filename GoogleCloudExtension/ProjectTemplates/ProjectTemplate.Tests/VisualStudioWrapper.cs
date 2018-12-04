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

using EnvDTE;
using EnvDTE80;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Process = System.Diagnostics.Process;
using Thread = System.Threading.Thread;

namespace ProjectTemplate.Tests
{
    /// <summary>
    /// A class that wraps a Visual Studio <see cref="Process"/>,
    /// the <see cref="DTE2"/> automation object,
    /// and a <see cref="ComMessageFilter"/>.
    /// </summary>
    public sealed class VisualStudioWrapper : IDisposable
    {
        private static readonly TimeSpan s_timeout = TimeSpan.FromMinutes(2);
        private static readonly int s_timeoutMills = (int)s_timeout.TotalMilliseconds;

        private readonly Process _devEnvProcess;
        private readonly ComMessageFilter _comMessageFilter;

        /// <summary>
        /// The automation object for the new Visual Studio instance.
        /// </summary>
        public DTE2 Dte { get; }

        private VisualStudioWrapper(string devEnvPath, string arguments)
        {
            // start devenv.exe
            _devEnvProcess = Process.Start(
                new ProcessStartInfo
                {
                    Arguments = $"-Embedding {arguments}",
                    CreateNoWindow = true,
                    FileName = devEnvPath,
                    WindowStyle = ProcessWindowStyle.Hidden
                });

            if (_devEnvProcess == null)
            {
                throw new InvalidOperationException($"Process for {devEnvPath} was not started.");
            }
            if (_devEnvProcess.HasExited)
            {
                throw new InvalidOperationException($"{devEnvPath} exited.");
            }
            Dte = GetRunningDte();
            _comMessageFilter = new ComMessageFilter();
        }

        ~VisualStudioWrapper()
        {
            Dispose(false);
        }

        /// <summary>
        /// Kills the Visual Studio process and unregisters the com message filter.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            try
            {
                Dte.Quit();
                _devEnvProcess.WaitForExit(s_timeoutMills);
            }
            catch (Exception e)
            {
                // Don't throw during disposal/finalization.
                Debug.WriteLine($"Error in {nameof(VisualStudioWrapper)}.{nameof(Dispose)}: {e}");
            }

            try
            {
                if (!_devEnvProcess.HasExited)
                {
                    _devEnvProcess.KillProcessTree();
                }
            }
            catch (Exception e)
            {
                // Don't throw during disposal/finalization.
                Debug.WriteLine($"Error in {nameof(VisualStudioWrapper)}.{nameof(Dispose)}: {e}");
            }

            // Dispose managed resource on disposal but not finalization.
            if (disposing)
            {
                _devEnvProcess.Dispose();
                _comMessageFilter.Dispose();
            }
        }

        /// <summary>
        /// Factory method for creating a new <see cref="VisualStudioWrapper"/> linked to an experimental instance.
        /// </summary>
        /// <returns>A new <see cref="VisualStudioWrapper"/> of the given version.</returns>
        public static VisualStudioWrapper CreateExperimentalInstance(string version)
        {
            string devEnvPath = GetDevEnvPath(version);
            return new VisualStudioWrapper(devEnvPath, "/rootSuffix Exp");
        }

        private static string GetDevEnvPath(string version)
        {
            var installRootValue = Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\SxS\VS7",
                version,
                null);
            if (installRootValue is string installRoot)
            {
                return Path.Combine(installRoot, @"Common7\IDE\devenv.exe");
            }
            else
            {
                var tempDte = (DTE)Marshal.GetActiveObject($"VisualStudio.DTE.{version}");
                return tempDte.FullName;
            }
        }

        private DTE2 GetRunningDte()
        {
            DateTimeOffset timeout = DateTimeOffset.Now.Add(s_timeout);
            while (timeout > DateTimeOffset.Now)
            {
                if (ComHelper.GetRunningObject(IsMatchingMonikerName) is DTE2 dte)
                {
                    return dte;
                }
                Thread.Sleep(200);
            }
            throw new TimeoutException("Running DTE not found.");
        }

        private bool IsMatchingMonikerName(IMoniker moniker, IBindCtx bindCtx)
        {
            try
            {
                moniker.GetDisplayName(bindCtx, null, out string name);
                return !string.IsNullOrWhiteSpace(name) &&
                    name.StartsWith("!VisualStudio.DTE", StringComparison.Ordinal) &&
                    name.EndsWith($":{_devEnvProcess.Id}", StringComparison.Ordinal);
            }
            catch (NotImplementedException)
            {
                return false;
            }
            catch (COMException)
            {
                return false;
            }
        }
    }
}
