using GoogleCloudExtension.GCloud.Models;
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

        public static async Task<IList<GkeService>> GetServicesAsync(KubectlContext context)
        {
            var services = await GetJsonOutputAsync<GkeList<GkeService>>("get services", context);
            return services.Items;
        }

        public static Task<GkeService> GetServiceAsync(string name, KubectlContext context)
        {
            return GetJsonOutputAsync<GkeService>($"get service {name}", context);
        }
            
        private static Task<bool> RunCommandAsync(string command, Action<string> outputAction, KubectlContext context)
        {
            var actualCommand = FormatCommand(command, context);
            Debug.WriteLine($"Executing kubectl command: kubectl {actualCommand}");
            return ProcessUtils.RunCommandAsync("kubectl", actualCommand, (o, e) => outputAction(e.Line));
        }

        private static async Task<T> GetJsonOutputAsync<T>(string command, KubectlContext context)
        {
            var actualCommand = FormatCommand(command, context, jsonOutput: true);
            try
            {
                Debug.WriteLine($"Executing kubectl command: kubectl {actualCommand}");
                return await ProcessUtils.GetJsonOutputAsync<T>("kubectl", actualCommand);
            }
            catch (JsonOutputException ex)
            {
                throw new GCloudException($"Failed to exectue command {actualCommand}\nInner exception: {ex.Message}", ex);
            }
        }

        private static string FormatCommand(string command, KubectlContext context, bool jsonOutput = false)
        {
            var format = jsonOutput ? "--output=json" : "";

            return $"{command} --kubeconfig=\"{context.Config}\" {format}";
        }
    }
}
