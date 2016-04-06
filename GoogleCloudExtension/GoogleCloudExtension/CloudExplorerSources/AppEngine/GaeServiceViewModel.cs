using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Credentials;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.DataSources.Models;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorerSources.AppEngine
{
    internal class GaeServiceViewModel : TreeHierarchy
    {
        private const string ServiceIconResourcePath = "CloudExplorerSources/AppEngine/Resources/ic_view_module.png";

        private static readonly Lazy<ImageSource> s_serviceIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadResource(ServiceIconResourcePath));

        private readonly AppEngineRootViewModel _owner;
        private readonly GaeService _service;
        private readonly WeakCommand _deleteCommand;

        public GaeServiceViewModel(AppEngineRootViewModel owner, GaeService service, IEnumerable<TreeNode> children) : base(children)
        {
            _owner = owner;
            _service = service;
            _deleteCommand = new WeakCommand(OnDeleteCommand);

            Content = _service.Id;
            Icon = s_serviceIcon.Value;

            var menuItems = new List<MenuItem>
            {
                new MenuItem { Header="Delete Service", Command = _deleteCommand },
            };
            ContextMenu = new ContextMenu { ItemsSource = menuItems };
        }

        private async void OnDeleteCommand()
        {
            if (!UserPromptUtils.YesNoPrompt($"Are you sure you want to delete service {_service.Id}?", $"Deleting service {_service.Id}"))
            {
                Debug.WriteLine($"User cancelled deletion of {_service.Id}");
                return;
            }

            try
            {
                var oauthToken = await CredentialsManager.GetAccessTokenAsync();

                _deleteCommand.CanExecuteCommand = false;
                Content = $"{_service.Id} (Deleting...)";
                IsLoading = true;

                await GaeDataSource.DeleteServiceAsync(
                    projectId: _owner.Owner.CurrentProject.Id,
                    serviceId: _service.Id,
                    oauthToken: oauthToken);

                _owner.Refresh();
            }
            catch (DataSourceException ex)
            {
                _deleteCommand.CanExecuteCommand = true;
                Content = _service.Id;
                IsLoading = false;

                Debug.WriteLine($"Failed to delete service {_service.Id}: {ex.Message}");
                GcpOutputWindow.Activate();
                GcpOutputWindow.OutputLine($"Failed to delete service {_service.Id}");
                GcpOutputWindow.OutputLine(ex.Message);
            }
        }
    }
}
