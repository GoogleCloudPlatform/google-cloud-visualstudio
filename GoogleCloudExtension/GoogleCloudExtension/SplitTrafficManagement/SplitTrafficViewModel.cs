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

using Google.Apis.Appengine.v1.Data;
using GoogleCloudExtension.AddTrafficSplit;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace GoogleCloudExtension.SplitTrafficManagement
{
    /// <summary>
    /// The changes made by the user in the <seealso cref="SplitTrafficWindow"/> dialog.
    /// </summary>
    public class SplitTrafficChange
    {
        /// <summary>
        /// The map of version Ids to traffic percentages.
        /// </summary>
        public IDictionary<string, double?> Allocations { get; }

        /// <summary>
        /// The mechanism to shard the traffic by.  Either "COOKIE" or "IP"
        /// </summary>
        public string ShardBy { get; }

        public SplitTrafficChange(IDictionary<string, double?> allocations, string shardBy)
        {
            Allocations = allocations;
            ShardBy = shardBy;
        }
    }

    /// <summary>
    /// This class is the view model for the <seealso cref="SplitTrafficWindow"/> dialog.
    /// </summary>
    public class SplitTrafficViewModel : ViewModelBase
    {
        private readonly SplitTrafficWindow _owner;
        private readonly Service _service;
        private readonly IList<string> _availableVersions;

        private bool _isIpChecked;
        private bool _isCookieChecked;
        private SplitTrafficModel _selectedSplit;

        /// <summary>
        /// Whether the user has checked IP address as the way to shard traffic. This is
        /// bound in the UI to a radio button.
        /// </summary>
        public bool IsIpChecked
        {
            get { return _isIpChecked; }
            set { SetValueAndRaise(ref _isIpChecked, value); }
        }

        /// <summary>
        /// Whether the user has checked cookies as the way to shard traffic. This is
        /// bound in the UI to a radio button.
        /// </summary>
        public bool IsCookieChecked
        {
            get { return _isCookieChecked; }
            set { SetValueAndRaise(ref _isCookieChecked, value); }
        }

        /// <summary>
        /// The list of traffic allocations.
        /// </summary>
        public ObservableCollection<SplitTrafficModel> Allocations { get; } = new ObservableCollection<SplitTrafficModel>();

        public SplitTrafficModel SelectedSplit
        {
            get { return _selectedSplit; }
            set
            {
                SetValueAndRaise(ref _selectedSplit, value);
                UpdateCommands();
            }
        }

        /// <summary>
        /// The changes that were made by the user. This property will be null if the user
        /// cancelled the dialog.
        /// </summary>
        public SplitTrafficChange Result { get; private set; }

        /// <summary>
        /// The command to execute when the Save button is pressed. Commits all of the changes.
        /// </summary>
        public ICommand SaveCommand { get; }

        /// <summary>
        /// The command to execute when the Delete allocation button is pressed.
        /// </summary>
        public ProtectedCommand DeleteAllocationCommand { get; }

        /// <summary>
        /// The command to execute when the Add Traffic Version button is pressed.
        /// </summary>
        public ProtectedCommand AddTrafficAllocationCommand { get; }

        public SplitTrafficViewModel(SplitTrafficWindow owner, Service service, IEnumerable<Google.Apis.Appengine.v1.Data.Version> versions)
        {
            _owner = owner;
            _service = service;

            // Set up the radio buttons properly.  Default to "IP" splitting if none is set.
            IsCookieChecked = GaeServiceExtensions.ShardByCookie.Equals(service?.Split?.ShardBy);
            IsIpChecked = !IsCookieChecked;

            // Set up the current allocations and avaible versions.
            Allocations = GetAllocations(service);
            _availableVersions = GetAvailableVersions(service, versions);

            SaveCommand = new ProtectedCommand(OnSaveCommand);
            DeleteAllocationCommand = new ProtectedCommand(OnDeleteCommand);
            AddTrafficAllocationCommand = new ProtectedCommand(OnAddTrafficAllocationCommand);

            UpdateCommands();
        }

        private void UpdateCommands()
        {
            AddTrafficAllocationCommand.CanExecuteCommand = _availableVersions.Count != 0;
            DeleteAllocationCommand.CanExecuteCommand = SelectedSplit != null;
        }

        /// <summary>
        /// Get a list of traffic allocations of a service's versions as <seealso cref="SplitTrafficModel"/>
        /// </summary>
        private ObservableCollection<SplitTrafficModel> GetAllocations(Service service)
        {
            IDictionary<string, double?> allocations = service?.Split?.Allocations ??
                new Dictionary<string, double?>();
            IEnumerable<SplitTrafficModel> models = allocations
                .Where(x => x.Value != null)
                .Select((x) => new SplitTrafficModel(x.Key, Convert.ToInt32(x.Value * 100)));
            return new ObservableCollection<SplitTrafficModel>(models);
        }

        /// <summary>
        /// Get a list of versions that are not allocated traffic in a service.
        /// </summary>
        private IList<string> GetAvailableVersions(
            Service service, IEnumerable<Google.Apis.Appengine.v1.Data.Version> versions)
        {
            ICollection<string> keys = service?.Split?.Allocations?.Keys;
            return versions
                .Where(x => !keys.Contains(x.Id))
                .Select(x => x.Id)
                .ToList();
        }

        private void OnAddTrafficAllocationCommand()
        {
            var result = AddTrafficSplitWindow.PromptUser(_availableVersions);
            if (result != null)
            {
                var allocation = new SplitTrafficModel(
                    versionId: result.Version,
                    trafficAllocation: result.Allocation);

                // Remove the allocation from the list of available allocations to in future invocations the
                // user can only select the available ones.
                _availableVersions.Remove(allocation.VersionId);

                // Add the allocation to the list.
                Allocations.Add(allocation);

                // Update the visual state.
                UpdateCommands();
            }
        }

        /// <summary>
        /// Called when the user clicks the Delete button.  This will remove the
        /// given version (<seealso cref="SplitTrafficModel.VersionId"/>>) from the list of
        /// <seealso cref="Allocations"/> and adds the version to the the list
        /// of <seealso cref="_availableVersions"/>.
        /// </summary>
        private void OnDeleteCommand()
        {
            var selected = SelectedSplit;
            Allocations.Remove(selected);
            _availableVersions.Add(selected.VersionId);

            // Update the visual state.
            UpdateCommands();
        }

        /// <summary>
        /// Called when the user clicks the Save button.  This handles error checks, 
        /// populates the <seealso cref="Result"/> field and closes the dialog.
        /// </summary>
        private void OnSaveCommand()
        {
            // Build the set of allocations.
            IDictionary<string, double?> allocations = new Dictionary<string, double?>();
            int sum = 0;
            foreach (var allocation in Allocations)
            {
                // Ensure each allocation have a valid traffic allocation percent.
                int percent = allocation.TrafficAllocation;
                if (percent <= 0 || percent > 100)
                {
                    UserPromptService.Default.ErrorPrompt(
                        message: String.Format(Resources.SplitTrafficWindowInvalidPercentRangeErrorMessage, allocation.VersionId),
                        title: Resources.SplitTrafficWindowInvalidPercentTitle);
                    return;
                }
                sum += percent;

                double doublePercent = Math.Round((double)allocation.TrafficAllocation / 100, 2);
                allocations.Add(allocation.VersionId, doublePercent);
            }

            // Ensure that 100% of traffic is allocated.
            if (sum != 100)
            {
                UserPromptService.Default.ErrorPrompt(
                    Resources.SplitTrafficWindowInvalidPercentSumErrorMessage,
                    Resources.SplitTrafficWindowInvalidPercentTitle);
                return;
            }

            string shardBy = IsIpChecked ? GaeServiceExtensions.ShardByIp : GaeServiceExtensions.ShardByCookie;
            Result = new SplitTrafficChange(allocations, shardBy);
            _owner.Close();
        }
    }
}
