using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics
{
    public enum CommandInvocationSource
    {
        None,
        ToolsMenu,
        ContextMenu,
        Button,
    }

    /// <summary>
    /// Centralized list of the commands in the extension. A command can be invoked from more than one location
    /// and be implemented in more than one class which is why it is important to have a single list so the
    /// resulting analytics are accurate.
    /// </summary>
    internal enum CommandName
    {
        // For the case no command is selected.
        None,

        // The command for opening the ManageAccounts dialog.
        OpenManageAccountsDialog,

        // The command for opening the cloud explorer.
        OpenCloudExplorerToolWindow,
        RefreshDataSource,
        ShowAllGceInstancesCommand,
        ShowOnlyWindowsGceInstancesCommand,
        OpenTerminalServerSessionForGceInstanceCommand,
        OpenWebsiteForGceInstanceCommand,
        GetPublishSettingsForGceInstance,
        OpenWebsiteForGcsBucket,
    }
}
