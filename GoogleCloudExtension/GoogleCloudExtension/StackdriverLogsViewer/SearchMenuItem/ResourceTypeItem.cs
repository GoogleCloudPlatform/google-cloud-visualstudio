using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using System.Collections.ObjectModel;
using System.Linq;
using GoogleCloudExtension.Utils;
using Google.Apis.Logging.v2.Data.Extensions;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    public class ResourceTypeItem : MenuItemViewModel
    {
        private LogsViewerViewModel LogsViewerModel => MenuItemParent as LogsViewerViewModel;

        public ResourceKeys ResourceTypeKeys { get; }

        public ResourceTypeItem(ResourceKeys resourceKeys, IMenuItem parent) : base(parent)
        {
            ResourceTypeKeys = resourceKeys;
            Header = ResourceTypeKeys.Type;
            IsSubmenuPopulated = ResourceTypeKeys.Keys?.FirstOrDefault() == null;
            // The resource type contains keys, add a fake item to make it submenuheader Role.
            if (!IsSubmenuPopulated)
            {
                MenuItems.Add(MenuItemViewModel.CreateFakeItem());
            }
        }

        private async Task<IEnumerable<string>> GceInstanceIdToName(IEnumerable<string> instanceIds)
        {
            var dataSource = new GceDataSource(
                CredentialsStore.Default.CurrentProjectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.ApplicationName);
            var allInstances = await dataSource.GetInstanceListAsync();
            return from id in instanceIds
                   join instance in allInstances on id equals instance.Id?.ToString() into joined
                   from subInstance in joined.DefaultIfEmpty()
                   orderby subInstance?.Name descending
                   select subInstance == null ? id : subInstance.Name;
        }

        protected override async Task LoadSubMenu()
        {
            var keys = await LogsViewerModel.GetResourceValues(ResourceTypeKeys);
            if (keys == null)
            {
                return;
            }

            IEnumerable<string> headers = keys;
            if (ResourceTypeKeys.Type == "gce_instance")
            {
                headers = await GceInstanceIdToName(keys);
            }

            foreach (var menuItem in headers.Select(x => new ResourceValueItem(x, this)))
            {
                MenuItems.Add(menuItem);
            }
        }
    }
}
