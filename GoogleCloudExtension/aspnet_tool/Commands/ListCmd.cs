// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using CommandLine;
using GoogleCloudExtension.DataSources;
using System;
using System.Linq;

namespace AspnetTool.Commands
{
    public class ListCmd : ICommand
    {
        public class Options : AuthenticatedOption, ICommandOptions
        {
            [Option('w', "only-windows", HelpText = "Only show windows ASP.NET servers.")]
            public bool OnlyWindows { get; set; }

            public ICommand CreateCommand()
            {
                return new ListCmd(this);
            }
        }

        private readonly Options _options;

        private ListCmd(Options options)
        {
            _options = options;
        }

        public int Execute()
        {
            //var instances = GceDataSource.GetInstanceListAsync(_options.ProjectId, _options.Token).Result;
            //int count = 0;

            //var results = _options.OnlyWindows
            //        ? instances.Where(x => x.IsAspnetInstance()) : instances;
            //foreach (var entry in results)
            //{
            //    Console.WriteLine($"  Name: {entry.Name} Ip: {entry.GetPublicIpAddress()} Zone: {entry.Zone}");
            //    ++count;
            //}
            //Console.WriteLine($"Instace(s): {count}");

            return 0;
        }
    }
}
