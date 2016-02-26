using GoogleCloudExtension.DataSources.Models;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    public static class ZoneOperationExtensions
    {
        public static async Task WaitForFinish(this ZoneOperation operation, string project, string zone, string oauthToken)
        {
            Debug.WriteLine($"Waiting on operation {operation.Name}");
            var client = new WebClient().SetOauthToken(oauthToken);
            var url = operation.SelfLink;
            Debug.WriteLine($"Checking operation: {url}");
            while (true)
            {
                var result = await client.DownloadStringTaskAsync(url);
                var newOperation = JsonConvert.DeserializeObject<ZoneOperation>(result);
                Debug.WriteLine($"Operation status: {newOperation.Status}");
                if (newOperation.Status == "DONE")
                {
                    if (newOperation.Error != null)
                    {
                        throw new ZoneOperationError(newOperation.Error);
                    }
                    return;
                }
                await Task.Delay(500);
            }
        }
    }
}
