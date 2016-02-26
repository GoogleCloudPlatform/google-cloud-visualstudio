using CommandLine;

namespace AspnetTool.Commands
{
    [CommandName("deploy")]
    class DeployCmd : ICommand
    {
        public class Options : AuthenticatedOption, ICommandOptions
        {
            [Option('i', "instance", HelpText = "The name of the instance where to deploy.", Required = true)]
            public string Instance { get; set; }

            public ICommand CreateCommand()
            {
                return new DeployCmd(this);
            }
        }

        private readonly Options _options;

        private DeployCmd(Options options)
        {
            _options = options;
        }

        public int Execute()
        {
            return 0;
        }
    }
}
