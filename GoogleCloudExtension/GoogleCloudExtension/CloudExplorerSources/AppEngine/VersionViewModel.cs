// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.AppEngine
{
    internal class VersionViewModel : TreeLeaf, ICloudExplorerItemSource
    {
        private const string IconResourcePath = "CloudExplorerSources/AppEngine/Resources/ic_web.png";
        private static readonly Lazy<ImageSource> s_versionIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(IconResourcePath));

        private readonly ModuleAndVersion _target;
        private readonly WeakCommand _openAppCommand;
        private readonly WeakCommand _deleteVersionCommand;
        private readonly WeakCommand _setDefaultVersionCommand;

        public object Item { get; }

        public VersionViewModel(ModuleAndVersion target)
        {
            _target = target;
            _openAppCommand = new WeakCommand(OnOpenApp);
            _deleteVersionCommand = new WeakCommand(OnDeleteVersion);
            _setDefaultVersionCommand = new WeakCommand(OnSetDefaultVersion, canExecuteCommand: _target.TrafficSplit != 1.0);

            Item = new ModuleAndVersionItem(_target);

            // Initialize the TreeLeaf properties.
            Content = target.Version;
            var menuItems = new List<MenuItem>
            {
                new MenuItem {Header="Open App in Browser", Command = _openAppCommand },
                new MenuItem {Header="Set as Default", Command = _setDefaultVersionCommand },
                new MenuItem {Header="Delete", Command = _deleteVersionCommand },
            };
            ContextMenu = new ContextMenu { ItemsSource = menuItems };
            Icon = s_versionIcon.Value;
        }

        #region Command handlers

        private async void OnOpenApp()
        {
            try
            {
                _openAppCommand.CanExecuteCommand = false;
                var accountAndProject = await GCloudWrapper.Instance.GetCurrentCredentialsAsync();
                var url = $"https://{_target.Version}-dot-{_target.Module}-dot-{accountAndProject.ProjectId}.appspot.com/";
                Debug.WriteLine($"Opening URL: {url}");
                Process.Start(url);
            }
            finally
            {
                _openAppCommand.CanExecuteCommand = true;
            }
        }

        private async void OnDeleteVersion()
        {
            try
            {
                _deleteVersionCommand.CanExecuteCommand = false;
                Content = $"{_target.Version} (Deleting...)";
                await AppEngineClient.DeleteAppVersion(_target.Module, _target.Version);
            }
            catch (GCloudException ex)
            {
                _deleteVersionCommand.CanExecuteCommand = true;
                GcpOutputWindow.OutputLine($"Failed to delete version {_target.Version} in module {_target.Module}");
                GcpOutputWindow.OutputLine(ex.Message);
                GcpOutputWindow.Activate();
            }
        }

        private async void OnSetDefaultVersion()
        {
            try
            {
                _setDefaultVersionCommand.CanExecuteCommand = false;
                Content = $"{_target.Version} (Setting as default...)";
                await AppEngineClient.SetDefaultAppVersionAsync(_target.Module, _target.Version);
            }
            catch (GCloudException ex)
            {
                _setDefaultVersionCommand.CanExecuteCommand = true;
                GcpOutputWindow.OutputLine("Failed to set default version.");
                GcpOutputWindow.OutputLine(ex.Message);
                GcpOutputWindow.Activate();
            }
        }

        #endregion
    }
}
