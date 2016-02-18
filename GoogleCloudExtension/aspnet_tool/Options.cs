using AspnetTool.Commands;
using CommandLine;
using CommandLine.Text;

namespace AspnetTool
{
    class Options
    {
        [VerbOption("deploy", HelpText = "Deploys the app")]
        public DeployCmd.Options Deploy { get; set; } = new DeployCmd.Options();

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }
}