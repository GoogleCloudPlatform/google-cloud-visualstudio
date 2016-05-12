# Design for the accounts manager subsystem
The account manager subsystem is responsible for storing, retrieving and in general all operations related to user accounts and credentials in the extension. This subsystem is also responsible for managing the OAUTH flow used to get user credentials.

## The AccountsManager class
This class is the main entry point for managing accounts in the extension. It leverates the `CredentialsStore` class to do the persistence of the accounts. This class offers the main interface to add and remove accounts from the store.

### The StartAddAccountFlowAsync() method
This method starts the OAUTH flow to request credentials for the user for the new account. If the flow is succesfull then the new account is added to the store and it is set as the _current_ account. An error is thrown if the new credentials are for an account already present in the store.

If the OAUTH flow fails and error message is shown to the user.

The user can always cancel out of the OAUTH flow, by closing the browser and pressing the cancel button on the dialog that opened. In this case no operation is performed.

### The DeleteAccount() method
This method just deletes the given account from the store.

## The CredentialsStore class
This class manages the credentials store for the extension. The store is persisted as files in the `%HOME%\Local Settings\googlecloudvsextension\accounts` directory.

Each file is a .json file that follows the same _schema_ as the default application credentials .json file for gcloud. Each file is named after the SHA1 has of the contents, to ensure uniqueness and safe file names.

The `CredentialsStore` class is designed to be a singleton, accessible through the `Default` static property. The singleton contains an in memory cache of all of the accounts stored in the store, and the current account and project id in use.

Because it is a singleton all parts of the extension that need to know what is the current user, or current project ID, can refer to the `Default` property to fetch it.

### The CurrentAccount and CurrentProjectId properties
These properties define the current set of credentials in use by the extension. Whenever these properties are set events are raised to notify interested parties that the credentials have changed; the cloud explorer uses these events to refresh itself whenever the user changes the active account, or project ID.

Whenever the `CurrentAccount` is set to a new value it will raise the `CurrentAccountChanged` event.

Whenever the `CurrentProjectId` is set to a new value it will raise the `CurrentProjectIdChanged` event.

Changing either the `CurrentAccount` or the `CurrentProjectId` also updates the last used credentials. These crentials are used when opening Visual Studio and no other credentials are selected. These last used credentials are stored in the `default_credentials` file.

### The CurrentGoogleCredential property
This property returns the GoogleCredential for the current account, which can be used with the Google client libraries.

### The AddAccount() and DeleteAccount() methods
The `AddAccount()` and `DeleteAccount()` methods add and remove respectively accounts from the store. Adding an account will also persist it into the system. Deleting an account will remove it from the system; if the deleted account is the current account it will also raise the `Reset` event to signify that no credentials are present.

### The ResetCredentials() method
The `ResetCredentials()` method will reset both the proejct ID and account in a single transaction. After they are changed the `Reset` event is raised.

### The GetAccount() method
The `GetAccount()` method will return the `UserAccount` that is associated with the given account name. This account name is typically the main email address associated with the account.

## The state classes
These classes are used to serialize to .json.

### The DefaultCredentials class
This class stores the credentials to use by default, when first opening Visual Studio and no other credentials are stored. It stores the project ID and account name, typically an email, for the credentials.

### The UserAccount class
This class contains the complete OAUTH credentials for a given account, including the refresh token, client_id and client_secret. This class is designed to be serialized to a .json file with an schema compatible with the application default credentials that gcloud consumes. These .json files are also consumable by gcloud using the --credential-file-override parameter.
