using GoogleCloudExtension.Analytics;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension
{
    public class AnalyticsOptionsPage : DialogPage
    {
        [Category("Usage Report")]
        [DisplayName("Report Usage Statistics Enabled")]
        [Description("Whether to report usage statistics to Google")]
        public bool OptIn { get; set; }

        [Browsable(false)]
        public bool DialogShown { get; set; }

        [Browsable(false)]
        public string ClientId { get; set; }

        public override void ResetSettings()
        {
            OptIn = false;
            DialogShown = false;
            ClientId = null;
        }

        /// <summary>
        /// Perform all of the setting alterantions necessary before finally saving the settings
        /// to storage.
        /// </summary>
        public override void SaveSettingsToStorage()
        {
            if (OptIn)
            {
                if (ClientId == null)
                {
                    Debug.WriteLine("Creating new Client ID");
                    ClientId = Guid.NewGuid().ToString();
                }
            }
            else
            {
                ClientId = null;
            }

            ExtensionAnalytics.AnalyticsOptInStateChanged();

            base.SaveSettingsToStorage();
        }
    }
}
