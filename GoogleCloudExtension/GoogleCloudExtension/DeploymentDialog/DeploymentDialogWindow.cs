// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GCloud;
using GoogleCloudExtension.Projects;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DeploymentDialog
{
    public class DeploymentDialogWindowOptions
    {
        public DnxProject Project { get; set; }
        public IList<DnxProject> ProjectsToRestore { get; set; }
    }

    public class DeploymentDialogWindow : DialogWindow
    {
        public DeploymentDialogWindow(DeploymentDialogWindowOptions options)
        {
            this.ResizeMode = System.Windows.ResizeMode.NoResize;
            this.Width = 420;
            this.Height = 370;
            this.Title = "Deploy to AppEngine";

            this.Options = options;

            var model = new DeploymentDialogViewModel(this);
            this.Content = new DeploymentDialogContent { DataContext = model };

            model.StartLoadingProjects();
        }

        public DeploymentDialogWindowOptions Options { get; private set; }
    }
}
