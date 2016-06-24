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

namespace GoogleCloudExtension.Analytics
{
    /// <summary>
    /// This enum specifies how a command is invoked.
    /// </summary>
    internal enum CommandInvocationSource
    {
        // No source.
        None,

        // The command is invoked from the Tools menu bar.
        ToolsMenu,

        // The command is invoked from a context menu.
        ContextMenu,

        // The command is invoked from a command button.
        Button,

        // The command is invoked by interacting with a list item.
        ListItem,
    }

    /// <summary>
    /// Centralized list of the commands in the extension. A command can be invoked from more than one location
    /// and be implemented in more than one class which is why it is important to have a single list so the
    /// resulting analytics are accurate.
    /// </summary>
    internal enum CommandName
    {
        // No command, default value.
        None,

        // Command to open the Manage Accounts dialog.
        OpenManageAccountsDialog,

        // Command to open the Cloud Explorer window.
        OpenCloudExplorerToolWindow,

        // Command to refresh the data sources in the Cloud Explorer.
        RefreshDataSource,

        // Command to show all GCE instances, vs. only Windows instances.
        ShowAllGceInstancesCommand,

        // Command to show only Windows GCE instances.
        ShowOnlyWindowsGceInstancesCommand,

        // Command to open a terminal server session against a Windows VM.
        OpenTerminalServerSessionForGceInstanceCommand,

        // Command to open the website served by a Windows VM.
        OpenWebsiteForGceInstanceCommand,

        // Command to get the publish settings for a Windows VM.
        GetPublishSettingsForGceInstance,

        // Command to open the website to browse a GCS bucket.
        OpenWebsiteForGcsBucket,

        // Command to cancel out of an OAUTH flow.
        OAuthFlowCancel,

        // Command invoked when double clicking an account list item.
        DoubleClickedAccountCommand,

        // Command to delete an account.
        DeleteAccountCommand,

        // Command to set the current account.
        SetCurrentAccountCommand,

        // Command to add a new account.
        AddAccountCommand,
    }
}
