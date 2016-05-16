using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension
{
    public class AnalyticsOptionsPage : DialogPage
    {
        [Category("Usage Report")]
        [DisplayName("Report Usage Enabled")]
        [Description("Whether to report usage statistics to Google")]
        public bool OptIn { get; set; }

        [Browsable(false)]
        public bool DialogShown { get; set; }

        public override void ResetSettings()
        {
            OptIn = false;
            DialogShown = false;
        }
    }
}
