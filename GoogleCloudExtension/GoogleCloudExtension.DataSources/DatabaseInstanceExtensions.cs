using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// This class implement extension methods which implement behaviors on top of the database instance models.
    /// </summary>
    public static class DatabaseInstanceExtensions
    {
        public const string RunnableState = "RUNNABLE";
        public const string SuspendedState = "SUSPENDED";
        public const string PendingCreateState = "PENDING_CREATE";
        public const string MaintenanceState = "MAINTENANCE";
        public const string UnknownState = "UNKNOWN_STATE";
    }
}
