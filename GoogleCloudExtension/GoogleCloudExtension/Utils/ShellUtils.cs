// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace GoogleCloudExtension.Utils
{
    public static class ShellUtils
    {
        public static void InvalidateCommandUIStatus()
        {
            // Invalidate the commands status.
            var shell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;
            if (shell == null)
            {
                return;
            }
            shell.UpdateCommandUI(0);
        }
    }
}
