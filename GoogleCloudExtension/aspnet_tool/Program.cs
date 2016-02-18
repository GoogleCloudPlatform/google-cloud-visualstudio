using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspnetTool
{
    class Program
    {
        static void Main(string[] args)
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
