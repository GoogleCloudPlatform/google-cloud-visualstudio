using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.DataSources;
using System.ComponentModel;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    public class AspNetInstanceItem : GceInstanceItem
    {
        private const string AspNetCategory = "ASP.NET Properties";

        public AspNetInstanceItem(Instance instance) : base(instance)
        { }

        [Category(AspNetCategory)]
        [Description("The password for the sa user in the SQL Server installed in the instance.")]
        public string SqlServerPassword => Instance.GetSqlServerPassword();
    }
}
