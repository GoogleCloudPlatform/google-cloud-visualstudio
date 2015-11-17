// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Dnx;
using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;

namespace GoogleCloudExtension.Utils
{
    internal static class CommandUtils
    {
        public static bool ValidateEnvironment(IServiceProvider serviceProvider)
        {
            var validDNXInstallation = DnxEnvironment.ValidateDnxInstallationForRuntime(DnxRuntime.DnxCore50) ||
                DnxEnvironment.ValidateDnxInstallationForRuntime(DnxRuntime.Dnx451);
            var validGCloudInstallation = GCloudWrapper.Instance.ValidateGCloudInstallation();
            var validEnvironment = validDNXInstallation && validGCloudInstallation;
            if (!validEnvironment)
            {
                Debug.WriteLine("Invoked when the environment is not valid.");
                VsShellUtilities.ShowMessageBox(
                    serviceProvider,
                    "Please ensure that GCloud is installed.",
                    "Error",
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_CRITICAL,
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
            return validEnvironment;
        }
    }
}
