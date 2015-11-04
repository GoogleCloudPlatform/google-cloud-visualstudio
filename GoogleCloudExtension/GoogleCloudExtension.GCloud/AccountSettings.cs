// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.GCloud
{
    public sealed class AccountSettings
    {
        [JsonProperty("accounts")]
        public IList<string> Accounts { get; set; }

        [JsonProperty("active_account")]
        public string ActiveAccount { get; set; }
    }
}
