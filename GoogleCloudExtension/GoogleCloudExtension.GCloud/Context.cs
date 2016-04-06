// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

namespace GoogleCloudExtension.GCloud
{
    /// <summary>
    /// This class holds the credentials used to perform GCloud operations.
    /// </summary>
    public sealed class Context
    {
        public string Account { get; }

        public string ProjectId { get; }

        public Context(string account = null, string projectId = null)
        {
            Account = account;
            ProjectId = projectId;
        }
    }
}
