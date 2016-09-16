using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics
{
    internal static class CommandStatusUtils
    {
        public const string StatusProperty = "status";

        public static string GetStatusString(CommandStatus status)
        {
            switch (status)
            {
                case CommandStatus.Failure:
                    return "failure";

                case CommandStatus.Success:
                    return "success";

                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
