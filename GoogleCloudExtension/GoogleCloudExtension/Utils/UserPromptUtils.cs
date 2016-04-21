using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace GoogleCloudExtension.Utils
{
    public static class UserPromptUtils
    {
        private const int IDYES = 6;
        private const int IDNO = 7;

        /// <summary>
        /// Show a message dialog with a Yes and No button to the user.
        /// </summary>
        /// <param name="message">The message for the dialog.</param>
        /// <param name="title">The title for the dialog.</param>
        /// <returns>Returns true if the user pressed the YES button.</returns>
        public static bool YesNoPrompt(string message, string title)
        {
            var result = VsShellUtilities.ShowMessageBox(
                    GoogleCloudExtensionPackage.Instance,
                    message,
                    title,
                    OLEMSGICON.OLEMSGICON_QUERY,
                    OLEMSGBUTTON.OLEMSGBUTTON_YESNO,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND);
            return result == IDYES;
        }

        /// <summary>
        /// Shows a message dialog to the user with an Ok button.
        /// </summary>
        /// <param name="message">The message for the dialog.</param>
        /// <param name="title">The title for the dialog.</param>
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

        /// <summary>
        /// Shows an error message dialog to the user, with an Ok button.
        /// </summary>
        /// <param name="message">The message for the dialog.</param>
        /// <param name="title">The title for the dialog.</param>
        public static void ErrorPrompt(string message, string title)
        {
            VsShellUtilities.ShowMessageBox(
                    GoogleCloudExtensionPackage.Instance,
                    message,
                    title,
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
