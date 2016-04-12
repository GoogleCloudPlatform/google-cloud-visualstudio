using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    public static class UserPromptUtils
    {
        public static bool YesNoPrompt(string message, string title)
        {
            var result = VsShellUtilities.ShowMessageBox(
                    GoogleCloudExtensionPackage.Instance,
                    message,
                    title,
                    OLEMSGICON.OLEMSGICON_QUERY,
                    OLEMSGBUTTON.OLEMSGBUTTON_YESNO,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND);
            return result == ButtonIds.IDYES;
        }

        public static void OkPrompt(string message, string title)
        {
            VsShellUtilities.ShowMessageBox(
                    GoogleCloudExtensionPackage.Instance,
                    message,
                    title,
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
