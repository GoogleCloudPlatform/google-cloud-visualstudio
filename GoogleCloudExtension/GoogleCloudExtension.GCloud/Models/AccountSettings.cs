// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.GCloud.Models
{
    /// <summary>
    /// This class represents the account settings as stored in the GCloud CLI store, it is used
    /// to deserialize the output of the CLI commands.
    /// </summary>
    internal sealed class AccountSettings
    {
        [JsonProperty("accounts")]
        public IList<string> Accounts { get; set; }

        [JsonProperty("active_account")]
        public string ActiveAccount { get; set; }
    }
}
