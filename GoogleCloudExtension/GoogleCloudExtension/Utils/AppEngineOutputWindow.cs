// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace GoogleCloudExtension.Utils
{
    public class AppEngineOutputWindow
    {
        private static readonly Guid s_windowGuid = new Guid("E701CE22-DDEA-418A-9E66-C5A4F3891958");
        private static readonly string s_windowTitle = "Google AppEngine";

        private static AppEngineOutputWindow s_instance;
        private static AppEngineOutputWindow Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = new AppEngineOutputWindow();
                }
                return s_instance;
            }
        }

        private IVsOutputWindowPane OutputWindowPane
        {
            get { return _outputWindowPane; }
        }

        private readonly IVsOutputWindow _outputWindow;
        private readonly IVsOutputWindowPane _outputWindowPane;

        private AppEngineOutputWindow()
        {
            _outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            _outputWindow.CreatePane(s_windowGuid, s_windowTitle, 1, 1);
            _outputWindow.GetPane(s_windowGuid, out _outputWindowPane);
        }

        public static void OutputString(string str)
        {
            Instance.OutputWindowPane.OutputString(str);
        }

        public static void OutputLine(string str)
        {
            Instance.OutputWindowPane.OutputString(str);
            Instance.OutputWindowPane.OutputString("\n");
        }

        public static void Activate()
        {
            Instance.OutputWindowPane.Activate();
        }

        public static void Clear()
        {
            Instance.OutputWindowPane.Clear();
        }
    }
}
