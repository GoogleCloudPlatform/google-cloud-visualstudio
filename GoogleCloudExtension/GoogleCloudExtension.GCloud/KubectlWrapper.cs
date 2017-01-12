using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GCloud
{
    public static class KubectlWrapper
    {
        public static Task<bool> CreateDeploymentAsync(
            string name,
            string image,
            Action<string> outputAction,
            KubectlContext context)
        {
            return RunCommandAsync($"run {name} --image={image} --port=8080 --record", outputAction, context);
        }

        public static Task<bool> ExposeServiceAsync(string deployment, Action<string> outputAction, KubectlContext context)
        {
            return RunCommandAsync(
                $"expose deployment {deployment} --port=80 --target-port=8080 --type=LoadBalancer",
                outputAction,
                context);
        }
            
        private static Task<bool> RunCommandAsync(string command, Action<string> outputAction, KubectlContext context)
        {
            var actualCommand = $"{command} --kubeconfig={context.Config}";
            Debug.WriteLine($"Executing kubectl command: kubectl {actualCommand}");
            return ProcessUtils.RunCommandAsync("kubectl", actualCommand, (o, e) => outputAction(e.Line));
        }
    }
}
