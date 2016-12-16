// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Google.Apis.Logging.v2.Data;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// Filters related controls view model of LogsViewerViewModel
    /// </summary>
    public partial class LogsViewerViewModel : ViewModelBase
    {
        private static readonly string[] s_defaultResourceSelections = new string[] { "global", "gce_instance" };

        private MonitoredResourceDescriptor _selectedResource;
        private IList<MonitoredResourceDescriptor> _resourceDescriptors;

        /// <summary>
        /// This is the filters combined by all selectors.
        /// </summary>
        private string _filter;

        /// <summary>
        /// Gets all resources types.
        /// </summary>
        public IList<MonitoredResourceDescriptor> ResourceDescriptors
        {
            get { return _resourceDescriptors; }
            private set { SetValueAndRaise(ref _resourceDescriptors, value); }
        }

        /// <summary>
        /// Gets or sets current selected resource types.
        /// </summary>
        public MonitoredResourceDescriptor SelectedResource
        {
            get { return _selectedResource; }
            set
            {
                if (value != null && _selectedResource != value)
                {
                    SetValueAndRaise(ref _selectedResource, value);
                    OnFiltersChanged();
                }
            }
        }

        /// <summary>
        /// Populate resource type selection list.
        /// 
        /// The control flow is as following. 
        ///     1. PopulateResourceTypes().
        ///         1.1 Failed. An error message is displayed. 
        ///              Goto error handling logic.
        ///     2. Set selected resource type.
        ///     3. When selected resource type is changed, it calls Reload().
        ///     
        /// Error handling.
        ///     1. User click Refresh. Refrsh button calls Reload().
        ///     2. Reload() checks ResourceDescriptors is null or empty.
        ///     3. Reload calls PopulateResourceTypes() which does a manual retry.
        /// </summary>
        private async void PopulateResourceTypes()
        {
            RequestErrorMessage = null;
            ShowRequestErrorMessage = false;
            IsControlEnabled = false;
            try
            {
                ResourceDescriptors = await _dataSource.Value.GetResourceDescriptorsAsync();
            }
            catch (DataSourceException ex)
            {
                /// If it fails, show a tip, let refresh button retry it.
                RequestErrorMessage = ex.Message;
                ShowRequestErrorMessage = true;
                return;
            }
            finally
            {
                IsControlEnabled = true;
            }

            foreach (var defaultSelection in s_defaultResourceSelections)
            {
                var desc = _resourceDescriptors?.First(x => x.Type == defaultSelection);
                if (desc != null)
                {
                    SelectedResource = desc;
                    return;
                }
            }

            // Select first one if type of global or gce_instance does not exists.
            SelectedResource = _resourceDescriptors?[0];
        }

        private void OnFiltersChanged()
        {
            Debug.WriteLine("NotifyFiltersChanged");
            _filter = ComposeSimpleFilters();
            Reload();
        }

        /// <summary>
        /// Aggregate all selections into filter string.
        /// </summary>
        private string ComposeSimpleFilters()
        {
            StringBuilder filter = new StringBuilder();
            if (_selectedResource != null)
            {
                filter.AppendLine($"resource.type=\"{_selectedResource.Type}\"");
            }

            return filter.Length > 0 ? filter.ToString() : null;
        }
    }
}
