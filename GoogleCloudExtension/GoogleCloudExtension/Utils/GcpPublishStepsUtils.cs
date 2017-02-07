using System;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Common utils for publishing steps to GCP.
    /// </summary>
    public static class GcpPublishStepsUtils
    {
        /// <summary>
        /// Returns a default version name suitable for publishing to GKE and Flex.
        /// </summary>
        /// <returns>The default name string.</returns>
        public static string GetDefaultVersion()
        {
            var now = DateTime.Now;
            return String.Format(
                "{0:0000}{1:00}{2:00}t{3:00}{4:00}{5:00}",
                now.Year, now.Month, now.Day,
                now.Hour, now.Minute, now.Second);
        }
    }
}
