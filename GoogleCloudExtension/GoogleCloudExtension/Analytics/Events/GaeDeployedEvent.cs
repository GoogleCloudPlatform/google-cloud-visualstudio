using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics.Events
{
    /// <summary>
    /// This event is sent after a deployment to App Engine Flex is finished. 
    /// </summary>
    internal static class GaeDeployedEvent
    {
        private const string GaeDeployedEventName = "flexDeployment";

        public static AnalyticsEvent Create(CommandStatus status)
        {
            return new AnalyticsEvent(
                GaeDeployedEventName,
                CommandStatusUtils.StatusProperty, CommandStatusUtils.GetStatusString(status));
        }
    }
}
