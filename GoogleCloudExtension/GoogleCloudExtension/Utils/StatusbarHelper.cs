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

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace GoogleCloudExtension.Utils
{
    public static class StatusbarHelper
    {
        private readonly static Lazy<IVsStatusbar> s_statusbar = new Lazy<IVsStatusbar>(
            () => Package.GetGlobalService(typeof(SVsStatusbar)) as IVsStatusbar);

        private static IVsStatusbar Statusbar => s_statusbar.Value;

        public static void SetText(string text)
        {
            try
            {
                int frozen;
                Statusbar.IsFrozen(out frozen);
                if (frozen != 0)
                {
                    return;
                }
                Statusbar.SetText(text);
            }
            catch (Exception ex)
            {
                ActivityLogUtils.LogError($"Failed to write to the status bar: {ex.Message}");
            }
        }

        public static IDisposable ShowDeployAnimation()
        {
            try
            {
                object animation = (short)Constants.SBAI_Deploy;
                Statusbar.Animation(1, ref animation);
                return new Disposable(HideDeployAnimation);
            }
            catch (Exception ex)
            {
                ActivityLogUtils.LogError($"Failed to show animation: {ex.Message}");
                return null;
            }
        }

        public static ProgressBarHelper ShowProgressBar(string label)
        {
            return new ProgressBarHelper(Statusbar, label);
        }

        private static void HideDeployAnimation()
        {
            try
            {
                object animation = (short)Constants.SBAI_Deploy;
                Statusbar.Animation(0, ref animation);
            }
            catch (Exception ex)
            {
                ActivityLogUtils.LogError($"Failed to hide animation: {ex.Message}");
            }
        }

        public static IDisposable Freeze()
        {
            try
            {
                Statusbar.FreezeOutput(1);
                return new Disposable(UnFreeze);
            }
            catch (Exception ex)
            {
                ActivityLogUtils.LogError($"Failed to freeze the status bar output: {ex.Message}");
                return null;
            }
        }

        private static void UnFreeze()
        {
            try
            {
                Statusbar.FreezeOutput(0);
            }
            catch (Exception ex)
            {
                ActivityLogUtils.LogError($"Failed to unfreeze the status bar output: {ex.Message}");
            }
        }
    }
}
