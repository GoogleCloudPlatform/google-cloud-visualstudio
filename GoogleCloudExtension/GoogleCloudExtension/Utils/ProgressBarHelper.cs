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

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Class that manages the progress bar in the status bar of Visual Studio.
    /// </summary>
    public class ProgressBarHelper : IDisposableProgress
    {
        private const uint Total = 1000;

        private readonly IVsStatusbar _statusbar;
        private readonly string _label;
        private uint _cookie;

        public ProgressBarHelper(IVsStatusbar statusbar, string label)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _statusbar = statusbar;
            _label = label ?? "";
            _cookie = 0;
            _statusbar.Progress(ref _cookie, 1, _label, 0, Total);
        }

        #region IDisposable

        public void Dispose() => GoogleCloudExtensionPackage.Instance.JoinableTaskFactory.Run(
            async () =>
            {
                await GoogleCloudExtensionPackage.Instance.JoinableTaskFactory.SwitchToMainThreadAsync();
                _statusbar.Progress(ref _cookie, 0, "", 0, 0);
            });

        #endregion

        #region IProgress<double>

        void IProgress<double>.Report(double value)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _statusbar.Progress(ref _cookie, 1, _label, (uint)(value * Total), Total);
        }

        #endregion
    }
}
