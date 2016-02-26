// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using CommandLine;

namespace AspnetTool.Commands
{
    public class AuthenticatedOption
    {
        [Option('t', "token", HelpText = "The OAuth token to use for authentication.")]
        public string Token { get; set; }

        [Option('p', "project-id", HelpText = "The project ID that contains the instances.", Required = true)]
        public string ProjectId { get; set; }
    }
}
