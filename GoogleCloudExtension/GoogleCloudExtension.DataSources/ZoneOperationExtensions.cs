// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.DataSources.Models;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    public static class ZoneOperationExtensions
    {
        public static async Task Wait(this ZoneOperation operation, string project, string zone, string oauthToken)
        {
            Debug.WriteLine($"Waiting on operation {operation.Name}");
            var client = new WebClient().SetOauthToken(oauthToken);
            var url = operation.SelfLink;
            Debug.WriteLine($"Checking operation: {url}");
            try
            {
                while (true)
                {
                    var result = await client.DownloadStringTaskAsync(url);
                    var newOperation = JsonConvert.DeserializeObject<ZoneOperation>(result);
                    Debug.WriteLine($"Operation status: {newOperation.Status}");
                    if (newOperation.Status == "DONE")
                    {
                        if (newOperation.Error != null)
                        {
                            throw new ZoneOperationException(newOperation.Error);
                        }
                        return;
                    }
                    await Task.Delay(500);
                }
            }
            catch (WebException ex)
            {
                Debug.WriteLine($"Failed to perform web request: {ex.Message}");
                throw new ZoneOperationException(ex.Message, ex);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Failed to parse response: {ex.Message}");
                throw new ZoneOperationException(ex.Message, ex);
            }
        }
    }
}
