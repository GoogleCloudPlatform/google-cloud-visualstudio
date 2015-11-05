// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

namespace GoogleCloudExtension.GCloud
{
    public sealed class AccountAndProjectId
    {
        public AccountAndProjectId(string account = null, string projectId = null)
        {
            this.Account = account;
            this.ProjectId = projectId;
        }

        public string Account { get; private set; }
        public string ProjectId { get; private set; }
    }
}
