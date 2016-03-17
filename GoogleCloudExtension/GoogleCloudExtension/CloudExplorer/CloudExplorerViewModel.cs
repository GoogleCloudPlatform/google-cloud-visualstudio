// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.ErrorDialogs;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorer
{
    /// <summary>
    /// This class contains the view specific logic for the AppEngineAppsToolWindow view.
    /// </summary>
    internal class CloudExplorerViewModel : ViewModelBase
    {
        private const string RefreshImagePath = "CloudExplorer/Resources/refresh.png";

        private static readonly Lazy<ImageSource> s_refreshIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(RefreshImagePath));

        private readonly IList<ICloudExplorerSource> _sources;
        private readonly List<ButtonDefinition> _buttons;
        private bool _isValidInstallation;
        private bool _validationErrorIsVisible;
        private string _vaidationErrorMessage;
        private ICommand _validationErrorActionCommand;

        /// <summary>
        /// The list of module and version combinations for the current project.
        /// </summary>
        public IEnumerable<TreeHierarchy> Roots
        {
            get
            {
                foreach (var source in _sources)
                {
                    yield return source.GetRoot();
                }
            }
        }

        public IList<ButtonDefinition> Buttons => _buttons;

        public ICommand ValidationErrorActionCommand
        {
            get { return _validationErrorActionCommand; }
            set { SetValueAndRaise(ref _validationErrorActionCommand, value); }
        }

        public bool IsValidInstallation
        {
            get { return _isValidInstallation; }
            set { SetValueAndRaise(ref _isValidInstallation, value); }
        }

        public bool ValidationErrorIsVisible
        {
            get { return _validationErrorIsVisible; }
            set { SetValueAndRaise(ref _validationErrorIsVisible, value); }
        }

        public string ValidationErrorMessage
        {
            get { return _vaidationErrorMessage; }
            set { SetValueAndRaise(ref _vaidationErrorMessage, value); }
        }

        public CloudExplorerViewModel(IEnumerable<ICloudExplorerSource> sources)
        {
            _sources = new List<ICloudExplorerSource>(sources);
            _buttons = new List<ButtonDefinition>()
            {
                new ButtonDefinition
                {
                    Icon = s_refreshIcon.Value,
                    ToolTip = "Refresh",
                    Command = new WeakCommand(this.OnRefresh),
                }
            };

            foreach (var source in _sources)
            {
                var sourceButtons = source.GetButtons();
                _buttons.AddRange(sourceButtons);
            }

            ValidateAndShowButtons(); 
        }

        private async void ValidateAndShowButtons()
        {
            var gcloudValidationResult = await EnvironmentUtils.ValidateGCloudInstallation();
            if (gcloudValidationResult.IsValidGCloudInstallation())
            {
                IsValidInstallation = true;
            }
            else
            {
                ValidationErrorMessage = gcloudValidationResult.GetDisplayString();
                ValidationErrorActionCommand = gcloudValidationResult.IsGCloudInstalled ?
                    new WeakCommand(OnInstallGCloudComponentsCommand) : new WeakCommand(OnInstallGCloudCommand);
                ValidationErrorIsVisible = true;
            }
        }

        private void OnInstallGCloudCommand()
        {
            Process.Start("https://cloud.google.com/sdk/gcloud/");
        }

        private async void OnInstallGCloudComponentsCommand()
        {
            var gcloudValidationResult = await EnvironmentUtils.ValidateGCloudInstallation();
            var errorDialog = new ValidationErrorDialogWindow(gcloudValidationResult: gcloudValidationResult);
            errorDialog.ShowDialog();
        }

        private void OnRefresh()
        {
            foreach (var source in _sources)
            {
                source.Refresh();
            }
            RaisePropertyChanged(nameof(Roots));
        }
    }
}
