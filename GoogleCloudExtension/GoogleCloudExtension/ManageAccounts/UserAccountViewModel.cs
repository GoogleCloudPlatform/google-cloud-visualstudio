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

using System;
using System.Threading.Tasks;
using Google.Apis.Plus.v1.Data;
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

        public IUserAccount UserAccount { get; }

        public bool IsCurrentAccount => CredentialsStore.Default.CurrentAccount?.AccountName == UserAccount.AccountName;

        public UserAccountViewModel(IUserAccount userAccount)
        {
            UserAccount = userAccount;

            AccountName = userAccount.AccountName;

            Task<Person> personTask;
            try
            {
                IDataSourceFactory dataSourceFactory = DataSourceFactory.Default;
                IGPlusDataSource dataSource = dataSourceFactory.CreatePlusDataSource(userAccount.GetGoogleCredential());
                personTask = dataSource.GetProfileAsync();
            }
            catch (Exception)
            {
                personTask = Task.FromResult<Person>(null);
            }


            // TODO: Show the default image while it is being loaded.
            ProfilePictureAsync = AsyncProperty.Create(personTask, x => x?.Image.Url);
            NameAsync = AsyncProperty.Create(personTask, x => x?.DisplayName, Resources.UiLoadingMessage);
        }
    }
}
