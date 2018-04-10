using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;

namespace GoogleCloudExtension.Extensions
{
    /// <summary>
    /// Extensions for WindowFrames
    /// </summary>
    public static class WindowFrameExtensions
    {
        /// <summary>
        /// Check whether a frame is truely onscreen.
        /// </summary>
        /// <param name="frame">The window frame</param>
        /// <returns>Whether frame is truely onscreen</returns>
        public static bool IsVisibleOnScreen(this IVsWindowFrame frame)
        {
            if (frame == null)
            {
                return false;
            }

            int onScreenFlag;
            Marshal.ThrowExceptionForHR(frame.IsOnScreen(out onScreenFlag));

            var windowOnScreen = Convert.ToBoolean(onScreenFlag);
            var windowNotMinimized = GoogleCloudExtensionPackage.Instance.IsWindowActive();

            return windowOnScreen && windowNotMinimized;
        }
    }
}
