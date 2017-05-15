using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class contains helpers for formatting strings.
    /// </summary>
    public static class StringFormatUtils
    {
        private const int MaxStringSize = 20;

        /// <summary>
        /// Formats a size in bytes into a human readable format.
        /// </summary>
        /// <param name="size">The size in bytes to format.</param>
        /// <returns>The human readable string, for example 3KB, 45 bytes, etc...</returns>
        public static string FormatByteSize(ulong size)
        {
            StringBuilder sb = new StringBuilder(MaxStringSize);
            var result = StrFormatByteSize(size, sb, sb.Capacity);
            if (result == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to convert value: {size}");
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
