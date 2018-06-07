using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.CloudResourceManager.v1.Data;

namespace GoogleCloudExtension.Accounts
{
    public interface ICredentialsStore
    {
        IEnumerable<UserAccount> AccountsList { get; }
        UserAccount CurrentAccount { get; }
        string CurrentAccountPath { get; }
        Task<IEnumerable<Project>> CurrentAccountProjects { get; }
        GoogleCredential CurrentGoogleCredential { get; }
        string CurrentProjectId { get; }
        string CurrentProjectNumericId { get; }

        event EventHandler CurrentAccountChanged;
        event EventHandler CurrentProjectIdChanged;
        event EventHandler CurrentProjectNumericIdChanged;
        event EventHandler Reset;

        void AddAccount(UserAccount userAccount);
        void DeleteAccount(UserAccount account);
        UserAccount GetAccount(string accountName);
        void RefreshProjects();
        void ResetCredentials(string accountName, string projectId);
        void UpdateCurrentAccount(UserAccount account);
        void UpdateCurrentProject(Project project);
    }
}