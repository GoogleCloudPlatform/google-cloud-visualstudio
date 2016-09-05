using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace GoogleCloudExtension.Utils
{
    public static class StatusbarHelper
    {
        private readonly static Lazy<IVsStatusbar> s_statusbar = new Lazy<IVsStatusbar>(
            () => Package.GetGlobalService(typeof(SVsStatusbar)) as IVsStatusbar);

        private static IVsStatusbar Statusbar => s_statusbar.Value;

        public static void SetText(string text)
        {
            try
            {
                int frozen;
                Statusbar.IsFrozen(out frozen);
                if (frozen != 0)
                {
                    return;
                }
                Statusbar.SetText(text);
            }
            catch (Exception ex)
            {
                ActivityLogUtils.LogError($"Failed to write to the status bar: {ex.Message}");
            }
        }

        public static IDisposable ShowDeployAnimation()
        {
            try
            {
                object animation = (short)Constants.SBAI_Deploy;
                Statusbar.Animation(1, ref animation);
                return new Disposable(HideDeployAnimation);
            }
            catch (Exception ex)
            {
                ActivityLogUtils.LogError($"Failed to show animation: {ex.Message}");
                return null;
            }
        }

        public static ProgressBarHelper ShowProgressBar()
        {
            return new ProgressBarHelper(Statusbar);
        }

        private static void HideDeployAnimation()
        {
            try
            {
                object animation = (short)Constants.SBAI_Deploy;
                Statusbar.Animation(0, ref animation);
            }
            catch (Exception ex)
            {
                ActivityLogUtils.LogError($"Failed to hide animation: {ex.Message}");
            }
        }

        public static IDisposable Freeze()
        {
            try
            {
                Statusbar.FreezeOutput(1);
                return new Disposable(UnFreeze);
            }
            catch (Exception ex)
            {
                ActivityLogUtils.LogError($"Failed to freeze the status bar output: {ex.Message}");
                return null;
            }
        }

        private static void UnFreeze()
        {
            try
            {
                Statusbar.FreezeOutput(0);
            }
            catch (Exception ex)
            {
                ActivityLogUtils.LogError($"Failed to unfreeze the status bar output: {ex.Message}");
            }
        }
    }
}
