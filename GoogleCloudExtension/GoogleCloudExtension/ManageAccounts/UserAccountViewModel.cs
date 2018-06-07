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

using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;

namespace GoogleCloudExtension.ManageAccounts
{
    public class UserAccountViewModel : Model
    {
        public AsyncProperty<string> ProfilePictureAsync { get; }

        public AsyncProperty<string> NameAsync { get; }

        public string AccountName { get; }

        public UserAccount UserAccount { get; }

        public bool IsCurrentAccount => CredentialsStore.Default.CurrentAccount?.AccountName == UserAccount.AccountName;

        public UserAccountViewModel(UserAccount userAccount)
        {
            UserAccount = userAccount;

            AccountName = userAccount.AccountName;

            var dataSource = new GPlusDataSource(userAccount.GetGoogleCredential(), GoogleCloudExtensionPackage.Instance.VersionedApplicationName);
            var personTask = dataSource.GetProfileAsync();

            // TODO: Show the default image while it is being loaded.
            ProfilePictureAsync = AsyncPropertyUtils.CreateAsyncProperty(personTask, x => x?.Image.Url);
            NameAsync = AsyncPropertyUtils.CreateAsyncProperty(personTask, x => x?.DisplayName, Resources.CloudExplorerLoadingMessage);
        }
    }
}
