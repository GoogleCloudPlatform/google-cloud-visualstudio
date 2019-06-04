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

using System;
using System.Runtime.CompilerServices;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Helper functions for Visual Studio Extension Package.
    /// </summary>
    public static class PackageUtils
    {
        /// <summary>
        /// Visual Studio Extension fails to load assembly when it is referenced by xaml file.
        /// This is a helper function that help load the assembly that contains the referenced type.
        /// 
        /// When the caller pass in the type, it is loaded automatically.
        /// The trick and sole purpose of this method is to disable the code optimization.
        /// 
        /// </summary>
        /// <param name="referencedType">the type used by the XAML file.</param>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static void ReferenceType(Type referencedType) => referencedType.ToString();
    }
}
