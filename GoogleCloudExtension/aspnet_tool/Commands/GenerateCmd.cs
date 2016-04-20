// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using CommandLine;
using GoogleCloudExtension.DataSources;
using System;
using System.IO;
using System.Linq;

namespace AspnetTool.Commands
{
    public class GenerateCmd : ICommand
    {
        public class Options : AuthenticatedOption, ICommandOptions
        {
            [Option('o', "output", HelpText = "Where to save the generated file.")]
            public string Output { get; set; }

            [Option('i', "instance", HelpText = "The name of the instance for which to generate publishettings.", Required = true)]
            public string Instance { get; set; }

            public ICommand CreateCommand()
            {
                return new GenerateCmd(this);
            }
        }

        private readonly Options _options;

        private GenerateCmd(Options options)
        {
            _options = options;
        }

        public int Execute()
        {
            //var instances = GceDataSource.GetInstanceListAsync(_options.ProjectId, _options.Token).Result;
            //var instance = instances.FirstOrDefault(x => x.Name == _options.Instance);
            //if (instance == null)
            //{
            //    Console.WriteLine($"ERROR: Cannot find {_options.Instance}");
            //    return -1;
            //}

            //var publishSettings = instance.GeneratePublishSettings();
            //if (String.IsNullOrEmpty(_options.Output))
            //{
            //    Console.WriteLine(publishSettings);
            //}
            //else
            //{
            //    File.WriteAllText(_options.Output, publishSettings);
            //}

            return 0;
        }
    }
}
