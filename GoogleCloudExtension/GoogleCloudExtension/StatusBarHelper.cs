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
            catch (Exception)
            {
                Debug.WriteLine("Failed to write to the status bar.");
            }
        }

        public static void ShowDeployAnimation()
        {
            try
            {
                object animation = (short)Constants.SBAI_Deploy;
                Statusbar.Animation(1, ref animation);
            }
            catch (Exception)
            {
                Debug.WriteLine("Failed to show animation.");
            }
        }

        public static void HideDeployAnimation()
        {
            try
            {
                object animation = (short)Constants.SBAI_Deploy;
                Statusbar.Animation(0, ref animation);
            }
            catch (Exception)
            {
                Debug.WriteLine("Failed to hide animation.");
            }
        }

        public static void Freeze()
        {
            try
            {
                Statusbar.FreezeOutput(1);
            }
            catch (Exception)
            { }
        }

        public static void UnFreeze()
        {
            try
            {
                Statusbar.FreezeOutput(0);
            }
            catch (Exception)
            { }
        }
    }
}
