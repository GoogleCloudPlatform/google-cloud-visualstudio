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

using System.Collections.Generic;

namespace GoogleCloudExtension.TemplateWizards.Dialogs.TemplateChooserDialog
{

    /// <summary>
    /// An object that describes the type of framework.
    /// </summary>
    public class FrameworkType
    {
        /// <summary>
        /// The moniker for .NET Core.
        /// </summary>
        public const string NetCoreAppMoniker = "netcoreapp";

        /// <summary>
        /// The moniker for .NET Framework.
        /// </summary>
        public const string NetFrameworkMoniker = "net";

        /// <summary>
        /// Defines .NET Core.
        /// </summary>
        public static readonly FrameworkType NetCoreApp = new FrameworkType(NetCoreAppMoniker);

        /// <summary>
        /// Defines .NET Framework.
        /// </summary>
        public static readonly FrameworkType NetFramework = new FrameworkType(NetFrameworkMoniker);

        /// <summary>
        /// The versionless moniker of this framework.
        /// </summary>
        public string Moniker { get; }

        /// <summary>
        /// Returns a list of available frameworks.
        /// </summary>
        public static IList<FrameworkType> GetAvailableFrameworks() => new List<FrameworkType> { NetCoreApp, NetFramework };

        private FrameworkType(string moniker)
        {
            Moniker = moniker;

        }

        /// <summary>
        /// Returns a localized, human readable description of the framework type.
        /// </summary>
        public override string ToString()
        {
            return Resources.ResourceManager.GetString($"FrameworkTypeDisplayName_{Moniker}", Resources.Culture);
        }
    }
}