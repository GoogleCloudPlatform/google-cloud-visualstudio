// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

namespace GoogleCloudExtension.GCloud
{
    /// <summary>
    /// This class holds the credentials used to perform GCloud operations.
    /// </summary>
    public sealed class Credentials
    {
        public Credentials(string account = null, string projectId = null)
        {
            this.Account = account;
            this.ProjectId = projectId;
        }

        public string Account { get; private set; }
        public string ProjectId { get; private set; }
    }
}
