﻿// Copyright 2018 Google Inc. All Rights Reserved.
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

namespace GoogleCloudExtension.Projects
{
    /// <summary>
    /// Constants for project properties and project item properties.
    /// </summary>
    public static class ProjectPropertyConstants
    {
        /// <summary>
        /// Constants for the CopyToOutputDirectory project item property.
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/dotnet/api/vslangproj80.fileproperties2.copytooutputdirectory">
        /// FileProperties2.CopyToOutputDirectory Property
        /// </seealso>
        public static class CopyToOutputDirectory
        {
            /// <summary>
            /// The name of the "Copy To Output Directory" project item property.
            /// </summary>
            public const string Name = "CopyToOutputDirectory";

            /// <summary>
            /// The "Do not copy" value.
            /// </summary>
            /// <seealso href="https://docs.microsoft.com/dotnet/api/vslangproj80.__copytooutputstate">
            /// __COPYTOOUTPUTSTATE enum
            /// </seealso>
            public const uint DoNotCopyValue = 0;

            /// <summary>
            /// The "Copy if newer" value.
            /// </summary>
            /// <seealso href="https://docs.microsoft.com/dotnet/api/vslangproj80.__copytooutputstate">
            /// __COPYTOOUTPUTSTATE enum
            /// </seealso>
            public const uint CopyIfNewerValue = 2;
        }
    }
}
