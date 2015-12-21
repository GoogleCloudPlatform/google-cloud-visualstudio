using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoogleCloudExtension.CloudExplorerSources
{
    internal class ModuleAndVersionViewModel : TreeLeaf
    {
        private readonly AppEngineSource _owner;
        private readonly ModuleAndVersion _target;
        private readonly WeakCommand _openAppCommand;
        private readonly WeakCommand _deleteVersionCommand;
        private readonly WeakCommand _setDefaultVersionCommand;

        public ModuleAndVersionViewModel(AppEngineSource owner, ModuleAndVersion target)
        {
            _owner = owner;
            _target = target;
            _openAppCommand = new WeakCommand(OnOpenApp);
            _deleteVersionCommand = new WeakCommand(OnDeleteVersion, canExecuteCommand: !_target.IsDefault);
            _setDefaultVersionCommand = new WeakCommand(OnSetDefaultVersion, canExecuteCommand: !_target.IsDefault);

            // Initialize the TreeLeaf properties.
            Content = target;
            var menuItems = new List<MenuItem>
            {
                new MenuItem {Header="Open App in Browser", Command = _openAppCommand },
                new MenuItem {Header="Set as Default", Command = _setDefaultVersionCommand },
                new MenuItem {Header="Delete", Command = _deleteVersionCommand },
            };
            ContextMenu = new ContextMenu { ItemsSource = menuItems };
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
                await AppEngineClient.DeleteAppVersion(_target.Module, _target.Version);
            }
            catch (GCloudException ex)
            {
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
                await AppEngineClient.SetDefaultAppVersionAsync(_target.Module, _target.Version);
            }
            catch (GCloudException ex)
            {
                AppEngineOutputWindow.OutputLine("Failed to set default version.");
                AppEngineOutputWindow.OutputLine(ex.Message);
                AppEngineOutputWindow.Activate();
            }
            _owner.LoadAppEngineAppListAsync();
        }

        #endregion
    }
}
