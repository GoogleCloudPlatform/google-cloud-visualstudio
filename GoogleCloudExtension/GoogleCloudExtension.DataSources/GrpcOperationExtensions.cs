using GoogleCloudExtension.DataSources.Models;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    internal static class GrpcOperationExtensions
    {
        public static async Task Wait(this GrpcOperation self, string oauthToken)
        {
            var baseUrl = $"https://appengine.googleapis.com/v1beta5/{self.Name}";
            try
            {
                var client = new WebClient().SetOauthToken(oauthToken);
                while (true)
                {
                    Debug.WriteLine($"Polling operation {self.Name} with URL {baseUrl}");
                    var response = await client.DownloadStringTaskAsync(baseUrl);
                    var operation = JsonConvert.DeserializeObject<GrpcOperation>(response);
                    if (operation.Done)
                    {
                        if (operation.Error != null)
                        {
                            throw new GrpcOperationException(operation.Error);
                        }
                        return;
                    }
                    // TODO: Perhaps specify the wait delay?
                    await Task.Delay(500);
                }
            }
            catch (WebException ex)
            {
                Debug.WriteLine($"Failed to poll: ${ex.Message}");
                throw new GrpcOperationException(ex.Message, ex);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Failed to parse reponse: {ex.Message}");
                throw new GrpcOperationException(ex.Message, ex);
            }
        }
    }
}
