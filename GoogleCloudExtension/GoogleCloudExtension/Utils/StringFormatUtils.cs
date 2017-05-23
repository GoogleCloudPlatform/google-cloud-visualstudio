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
using System.Runtime.InteropServices;
using System.Text;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class contains helpers for formatting strings.
    /// </summary>
    public static class StringFormatUtils
    {
        // This constant determines the maximum size of a size (in bytes) formatted for human
        // consumption.
        private const int MaxByteFormatStringSize = 20;

        /// <summary>
        /// Formats a size in bytes into a human readable format.
        /// </summary>
        /// <param name="size">The size in bytes to format.</param>
        /// <returns>The human readable string, for example 3KB, 45 bytes, etc...</returns>
        public static string FormatByteSize(ulong size)
        {
            StringBuilder sb = new StringBuilder(MaxByteFormatStringSize);
            var result = StrFormatByteSize(size, sb, sb.Capacity);
            if (result == IntPtr.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }
            return sb.ToString();
        }

        /// <summary>
        /// This pinvoke declaration uses the shell api to format the string, using the same routine that
        /// the Windows explorer would use.
        /// </summary>
        [DllImport("Shlwapi.dll", CharSet = CharSet.Unicode, EntryPoint = "StrFormatByteSizeW")]
        private static extern IntPtr StrFormatByteSize(
            ulong fileSize,
            [MarshalAs(UnmanagedType.LPTStr)] StringBuilder buffer,
            int bufferSize);
    }
}
