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

using Google.Apis.SQLAdmin.v1beta4.Data;
using GoogleCloudExtension.Utils;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows.Input;

namespace GoogleCloudExtension.AuthorizedNetworkManagement
{
    /// <summary>
    /// The changes made by the user in the <seealso cref="AuthorizedNetworksWindow"/> dialog.
    /// </summary>
    public class AuthorizedNetworkChange
    {
        /// <summary>
        /// A list of all of the authorized networks and any changes that may have occured.
        /// </summary>
        public IList<AclEntry> AuthorizedNetworks;

        /// <summary>
        /// True if any changes to the authorized networks occured.
        /// </summary>
        public bool HasChanges;

        public AuthorizedNetworkChange(IList<AuthorizedNetworkModel> authorizedNetworks)
        {
            // Check if any authorized networks are new or were updated.
            HasChanges = authorizedNetworks.Any(x => x.NewOrUpdated);

            // Get a list of all of the networks that weren't deleted.
            AuthorizedNetworks = authorizedNetworks.Where(x => !x.Deleted)
                .Select(x => new AclEntry { Name = x.Name, Value = x.Value }).ToList();
        }
    }

    /// <summary>
    /// This class is the view model for the <seealso cref="AuthorizedNetworksWindow"/> dialog.
    /// </summary>
    internal class AuthorizedNetworksViewModel : ViewModelBase
    {
        private readonly AuthorizedNetworksWindow _owner;

        private string _networkName;
        private string _networkValue;

        /// <summary>
        /// The list of authorized networks.
        /// </summary>
        public ObservableCollection<AuthorizedNetworkModel> Networks { get; } = new ObservableCollection<AuthorizedNetworkModel>();


        /// <summary>
        /// The network name, this is bound to an text box in the UI to allow the 
        /// user to add new networks.
        /// </summary>
        public string NetworkName
        {
            get { return _networkName; }
            set { SetValueAndRaise(ref _networkName, value); }
        }

        /// <summary>
        /// The network value, this is bound to an text box in the UI to allow the 
        /// user to add new networks.
        /// </summary>
        public string NetworkValue
        {
            get { return _networkValue; }
            set { SetValueAndRaise(ref _networkValue, value); }
        }

        /// <summary>
        /// The changes that were made by the user. This property will be null if the user
        /// cancelled the dialog.
        /// </summary>
        public AuthorizedNetworkChange Result { get; private set; }

        /// <summary>
        /// The command to execute when the Save button is pressed.
        /// </summary>
        public ICommand SaveCommand { get; }

        /// <summary>
        /// The command to execute when the Add Authorized Network button is pressed.
        /// </summary>
        public ICommand AddNetwork { get; }

        public AuthorizedNetworksViewModel(AuthorizedNetworksWindow owner, DatabaseInstance instance)
        {
            _owner = owner;
            foreach (var network in GetAuthorizedNetworks(instance))
            {
                Networks.Add(network);
            }
            Result = null;

            SaveCommand = new WeakCommand(OnSaveCommand);
            AddNetwork = new WeakCommand(OnAddNetwork);
        }

        /// <summary>
        /// Convert a list of <seealso cref="AclEntry"/>s to <seealso cref="AuthorizedNetworkModel"/>s.  Used
        /// to more easily display and manipulate the entries in the UI.
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        private ObservableCollection<AuthorizedNetworkModel> GetAuthorizedNetworks(DatabaseInstance instance)
        {
            IEnumerable<AclEntry> acls = instance?.Settings?.IpConfiguration?.AuthorizedNetworks ??
                                         Enumerable.Empty<AclEntry>();
            return new ObservableCollection<AuthorizedNetworkModel>(acls.Select((x) => new AuthorizedNetworkModel(x)));
        }


        /// <summary>
        /// Called when the user clicks the Add Authorized Network button.  This ensures that the
        /// IP address entred is valid and an IPv4 address.  It then adds the network to the
        /// current list and resets in the input fields.
        /// </summary>
        private void OnAddNetwork()
        {
            IPAddress address;
            if (!IPAddress.TryParse(NetworkValue, out address) || address.AddressFamily != AddressFamily.InterNetwork)
            {
                // TODO(talarico): Use form handler to detect errors before the user clicks.
                UserPromptUtils.ErrorPrompt(
                    Resources.AuthorizedNetworksWindowInvalidCidrFormatErrorMessage,
                    Resources.AuthorizedNetworksWindowInvalidCidrFormatErrorTitle);
                return;
            }

            AclEntry acl = new AclEntry()
            {
                Name = NetworkName,
                Value = NetworkValue
            };
            Networks.Add(new AuthorizedNetworkModel(acl));

            NetworkValue = "";
            NetworkName = "";
        }

        /// <summary>
        /// Called when the user clicks the Save button.  This populates the results field and closes the dialog.
        /// </summary>
        private void OnSaveCommand()
        {
            Result = new AuthorizedNetworkChange(Networks);
            _owner.Close();
        }
    }
}
