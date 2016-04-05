// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleCloudExtension.CredentialsManagement
{
    /// <summary>
    /// This class is the view model for the user and project list window.
    /// </summary>
    public class ManageCredentialsViewModel : ViewModelBase
    {
        private const string UpdateCurrentProjectCommand = nameof(UpdateCurrentProjectCommand);
        private const string UpdateCurrentAccountCommand = nameof(UpdateCurrentAccountCommand);

        private AsyncPropertyValue<IEnumerable<string>> _accountsAsync;
        private string _currentAccount;

        /// <summary>
        /// The list of registered accounts with GCloud.
        /// </summary>
        public AsyncPropertyValue<IEnumerable<string>> AccountsAsync
        {
            get { return _accountsAsync; }
            set { SetValueAndRaise(ref _accountsAsync, value); }
        }

        /// <summary>
        /// The selected account, setting this property changes the current account for the extension.
        /// </summary>
        public string CurrentAccount
        {
            get { return _currentAccount; }
            set
            {
                SetValueAndRaise(ref _currentAccount, value);
                UpdateCurrentAccount(value);
            }
        }

        public ManageCredentialsViewModel()
        {
            AccountsAsync = new AsyncPropertyValue<IEnumerable<string>>(GCloudWrapper.Instance.GetAccountsAsync());
        }
       
        /// <summary>
        /// Updates the current account if it has changed.
        /// </summary>
        /// <param name="value"></param>
        private void UpdateCurrentAccount(string value)
        {
        }
    }
}
