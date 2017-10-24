using Google.Apis.Appengine.v1.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// This class containes extension useable for <seealso cref="Location"/> instances.
    /// </summary>
    public static class GaeLocationExtensions
    {
        public static bool IsFlexEnabled(this Location self)
        {
            object value;
            if (!self.Metadata.TryGetValue("flexibleEnvironmentAvailable", out value))
            {
                return false;
            }

            return (bool)value;
        }

        public static string GetDisplayName(this Location self)
        {
            string[] parts = self.Name.Split('/');
            return parts.Last();
        }
    }
}
