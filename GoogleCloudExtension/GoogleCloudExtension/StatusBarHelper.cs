// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;

namespace GoogleCloudExtension
{
    public static class StatusbarHelper
    {
        private static IVsStatusbar s_statusbar;

        private static IVsStatusbar Statusbar
        {
            get
            {
                if (s_statusbar == null)
                {
                    s_statusbar = Package.GetGlobalService(typeof(SVsStatusbar)) as IVsStatusbar;
                }
                return s_statusbar;
            }
        }

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
                Debug.WriteLine($"Failed to write to the status bar: {ex.Message}");
            }
        }

        public static void ShowDeployAnimation()
        {
            try
            {
                object animation = (short)Constants.SBAI_Deploy;
                Statusbar.Animation(1, ref animation);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to show animation: {ex.Message}");
            }
        }

        public static void HideDeployAnimation()
        {
            try
            {
                object animation = (short)Constants.SBAI_Deploy;
                Statusbar.Animation(0, ref animation);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to hide animation: {ex.Message}");
            }
        }

        public static void Freeze()
        {
            try
            {
                Statusbar.FreezeOutput(1);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to freeze the status bar output: {ex.Message}");
            }
        }

        public static void UnFreeze()
        {
            try
            {
                Statusbar.FreezeOutput(0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to unfreeze the status bar output: {ex.Message}");
            }
        }
    }
}
