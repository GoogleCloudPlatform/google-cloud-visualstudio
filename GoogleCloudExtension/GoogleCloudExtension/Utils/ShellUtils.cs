using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace GoogleCloudExtension.Utils
{
    public static class ShellUtils
    {
        public static void InvalidateCommandUIStatus()
        {
            // Invalidate the commands status.                                                                                                                                                                                                
            var shell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;
            if (shell == null)
            {
                return;
            }
            shell.UpdateCommandUI(0);
        }
    }
}