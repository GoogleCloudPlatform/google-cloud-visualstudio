using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    public static class FileSizeUtils
    {
        public static string FormatSize(ulong size)
        {
            StringBuilder sb = new StringBuilder(11);
            var result = StrFormatByteSize(size, sb, sb.Capacity);
            if (result == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to convert value: {size}");
            }
            return sb.ToString();
        }

        [DllImport("Shlwapi.dll", CharSet = CharSet.Unicode, EntryPoint = "StrFormatByteSizeW")]
        private static extern IntPtr StrFormatByteSize(
            ulong fileSize,
            [MarshalAs(UnmanagedType.LPTStr)] StringBuilder buffer,
            int bufferSize);
    }
}
