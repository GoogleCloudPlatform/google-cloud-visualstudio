using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.DataSources.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
