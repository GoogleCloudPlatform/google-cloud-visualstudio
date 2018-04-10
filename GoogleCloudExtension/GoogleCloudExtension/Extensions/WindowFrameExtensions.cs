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
            // Noop if we cant get hold of WindowFrame
            var onScreenFlag = 1;
            frame?.IsOnScreen(out onScreenFlag);

            var windowOnScreen = onScreenFlag == 1;
            var windowNotMinimized = GoogleCloudExtensionPackage.Instance.IsWindowActive();

            return windowOnScreen && windowNotMinimized;
        }
    }
}
