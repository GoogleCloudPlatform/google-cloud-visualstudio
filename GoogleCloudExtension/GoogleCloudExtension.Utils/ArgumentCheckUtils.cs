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

namespace GoogleCloudExtension.Utils
{
    public static class ArgumentCheckUtils
    {
        public static string ThrowIfNullOrEmpty(this string arg, string message)
        {
            if (String.IsNullOrWhiteSpace(arg))
            {
                throw new ArgumentException(message ?? "");
            }

            return arg;
        }

        public static T ThrowIfNull<T>(this T arg, string paramName)
        {
            if (arg == null)
            {
                throw new ArgumentNullException(paramName ?? "");
            }

            return arg;
        }
    }
}
