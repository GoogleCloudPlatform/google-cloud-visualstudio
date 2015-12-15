// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Dnx;
using System;

namespace GoogleCloudExtension.Utils
{
    internal static class CommandUtils
    {
        public static bool ValidateEnvironment(IServiceProvider serviceProvider)
        {
            var validDNXInstallation = DnxEnvironment.ValidateDnxInstallation();
            var validGCloudInstallation = GCloudWrapper.Instance.ValidateGCloudInstallation();
            var validEnvironment = validDNXInstallation && validGCloudInstallation;
            return validEnvironment;
        }
    }
}
