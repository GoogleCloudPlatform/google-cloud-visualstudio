// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud.Dnx;
using Microsoft.VisualStudio.PlatformUI;
using System.Collections.Generic;

namespace GoogleCloudExtension.DeploymentDialog
{
    public class DeploymentDialogWindowOptions
    {
        public Project Project { get; set; }
        public IList<Project> ProjectsToRestore { get; set; }
    }

    public class DeploymentDialogWindow : DialogWindow
    {
        public DeploymentDialogWindow(DeploymentDialogWindowOptions options)
        {
            this.ResizeMode = System.Windows.ResizeMode.NoResize;
            this.Width = 420;
            this.Height = 400;
            this.Title = "Deploy to AppEngine";

            this.Options = options;

            var model = new DeploymentDialogViewModel(this);
            this.Content = new DeploymentDialogContent { DataContext = model };

            model.StartLoadingProjectsAsync();
        }

        public DeploymentDialogWindowOptions Options { get; private set; }
    }
}
