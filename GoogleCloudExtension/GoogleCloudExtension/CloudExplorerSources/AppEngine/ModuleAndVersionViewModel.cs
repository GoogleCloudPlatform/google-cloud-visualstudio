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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GoogleCloudExtension.CloudExplorerSources.AppEngine
{
    internal class ModuleAndVersionViewModel : TreeLeaf, ICloudExplorerItemSource
    {
        private const string IconResourcePath = "CloudExplorerSources/AppEngine/Resources/ic_web.png";
        private static readonly Lazy<ImageSource> s_versionIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(IconResourcePath));

        private readonly AppEngineSource _owner;
        private readonly ModuleAndVersion _target;
        private readonly WeakCommand _openAppCommand;
        private readonly WeakCommand _deleteVersionCommand;
        private readonly WeakCommand _setDefaultVersionCommand;

        public object Item { get; }

        public ModuleAndVersionViewModel(AppEngineSource owner, ModuleAndVersion target)
        {
            _owner = owner;
            _target = target;
            _openAppCommand = new WeakCommand(OnOpenApp);
            _deleteVersionCommand = new WeakCommand(OnDeleteVersion, canExecuteCommand: !_target.IsDefault);
            _setDefaultVersionCommand = new WeakCommand(OnSetDefaultVersion, canExecuteCommand: !_target.IsDefault);

            Item = new ModuleAndVersionItem(_target);

            // Initialize the TreeLeaf properties.
            Content = FormatDisplayString(target);
            var menuItems = new List<MenuItem>
            {
                new MenuItem {Header="Open App in Browser", Command = _openAppCommand },
                new MenuItem {Header="Set as Default", Command = _setDefaultVersionCommand },
                new MenuItem {Header="Delete", Command = _deleteVersionCommand },
            };
            ContextMenu = new ContextMenu { ItemsSource = menuItems };
            Icon = s_versionIcon.Value;
        }

        private static string FormatDisplayString(ModuleAndVersion target)
        {
            var isDefault = target.IsDefault ? "(Default)" : "";
            return $"{target.Version} {isDefault}";
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
                AppEngineOutputWindow.OutputLine($"Failed to delete version {_target.Version} in module {_target.Module}");
                AppEngineOutputWindow.OutputLine(ex.Message);
                AppEngineOutputWindow.Activate();
            }
            _owner.LoadAppEngineAppListAsync();
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
                AppEngineOutputWindow.OutputLine("Failed to set default version.");
                AppEngineOutputWindow.OutputLine(ex.Message);
                AppEngineOutputWindow.Activate();
            }
            _owner.LoadAppEngineAppListAsync();
        }

        #endregion
    }
}
