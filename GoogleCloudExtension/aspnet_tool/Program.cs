// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;
using System.Diagnostics;

namespace AspnetTool
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var options = new Options();
            if (!CommandLine.Parser.Default.ParseArguments(
                args,
                options,
                (verb, subOptions) =>
                {
                    Debug.WriteLine($"Executing {verb}");
                    var cmdOptions = subOptions as ICommandOptions;
                    if (cmdOptions == null)
                    {
                        Console.WriteLine($"Unknown command: {verb}");
                        Environment.Exit(-1);
                    }

                    var cmd = cmdOptions.CreateCommand();
                    Environment.Exit(cmd.Execute());
                }))
            {
                Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
            }
        }
    }
}
