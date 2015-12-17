using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoogleCloudExtension.AppEngineApps
{
    internal class ModuleAndVersionViewModel: Model
    {
        private readonly AppEngineAppsToolViewModel _owner;

        public ModuleAndVersion ModuleAndVersion { get; }

        /// <summary>
        /// The command to invoke to open a browser on the selected app.
        /// </summary>
        public WeakCommand OpenAppCommand { get; }

        /// <summary>
        /// The command to invoke to delete the selected version.
        /// </summary>
        public WeakCommand DeleteVersionCommand { get; }

        /// <summary>
        /// The command to invoke to set the selected version as the default version.
        /// </summary>
        public WeakCommand SetDefaultVersionCommand { get; }

        public ModuleAndVersionViewModel(AppEngineAppsToolViewModel owner, ModuleAndVersion version)
        {
            _owner = owner;
            ModuleAndVersion = version;
            OpenAppCommand = new WeakCommand(OnOpenApp);
            DeleteVersionCommand = new WeakCommand(OnDeleteVersion, canExecuteCommand: !version.IsDefault);
            SetDefaultVersionCommand = new WeakCommand(OnSetDefaultVersion, canExecuteCommand: !version.IsDefault);
        }

        #region Command handlers

        private async void OnOpenApp()
        {
            try
            {
                OpenAppCommand.CanExecuteCommand = false;
                var accountAndProject = await GCloudWrapper.Instance.GetCurrentCredentialsAsync();
                var url = $"https://{ModuleAndVersion.Version}-dot-{ModuleAndVersion.Module}-dot-{accountAndProject.ProjectId}.appspot.com/";
                Debug.WriteLine($"Opening URL: {url}");
                Process.Start(url);
            }
            finally
            {
                OpenAppCommand.CanExecuteCommand = true;
            }
        }

        private async void OnDeleteVersion()
        {
            try
            {
                _owner.LoadingMessage = "Deleting version...";
                _owner.Loading = true;
                await AppEngineClient.DeleteAppVersion(ModuleAndVersion.Module, ModuleAndVersion.Version);
            }
            catch (GCloudException ex)
            {
                AppEngineOutputWindow.OutputLine($"Failed to delete version {ModuleAndVersion.Version} in module {ModuleAndVersion.Module}");
                AppEngineOutputWindow.OutputLine(ex.Message);
                AppEngineOutputWindow.Activate();
            }
            finally
            {
                _owner.Loading = false;
            }
            _owner.LoadAppEngineAppListAsync();
        }

        private async void OnSetDefaultVersion()
        {
            try
            {
                _owner.Loading = true;
                _owner.LoadingMessage = "Setting default version...";
                await AppEngineClient.SetDefaultAppVersionAsync(ModuleAndVersion.Module, ModuleAndVersion.Version);
            }
            catch (GCloudException ex)
            {
                AppEngineOutputWindow.OutputLine("Failed to set default version.");
                AppEngineOutputWindow.OutputLine(ex.Message);
                AppEngineOutputWindow.Activate();
            }
            finally
            {
                _owner.Loading = false;
            }
            _owner.LoadAppEngineAppListAsync();
        }


        #endregion
    }
}
