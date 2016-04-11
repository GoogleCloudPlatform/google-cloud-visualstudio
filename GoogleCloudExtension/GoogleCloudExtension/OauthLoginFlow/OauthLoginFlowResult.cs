using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.OauthLoginFlow
{
    class OauthLoginFlowResult
    {
        /// <summary>
        /// Output property, set from the window once the access code is known.
        /// </summary>
        public string AccessCode { get; set; }

        /// <summary>
        /// Output property, set from the window once the flow is done.
        /// </summary>
        public bool IsSuccess { get; set; }
    }
}
