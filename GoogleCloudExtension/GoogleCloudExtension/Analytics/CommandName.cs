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
        ListItem,
    }

    /// <summary>
    /// Centralized list of the commands in the extension. A command can be invoked from more than one location
    /// and be implemented in more than one class which is why it is important to have a single list so the
    /// resulting analytics are accurate.
    /// </summary>
    internal enum CommandName
    {
        None,
        OpenManageAccountsDialog,
        OpenCloudExplorerToolWindow,
        RefreshDataSource,
        ShowAllGceInstancesCommand,
        ShowOnlyWindowsGceInstancesCommand,
        OpenTerminalServerSessionForGceInstanceCommand,
        OpenWebsiteForGceInstanceCommand,
        GetPublishSettingsForGceInstance,
        OpenWebsiteForGcsBucket,
        OAuthFlowCancel,
        DoubleClickedAccountCommand,
        DeleteAccountCommand,
        SetCurrentAccountCommand,
        AddAccountCommand,
    }
}
