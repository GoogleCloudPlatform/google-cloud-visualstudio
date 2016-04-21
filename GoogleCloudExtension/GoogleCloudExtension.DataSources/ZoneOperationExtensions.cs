// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Google;
using Google.Apis.Compute.v1;
using Google.Apis.Compute.v1.Data;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    public static class ZoneOperationExtensions
    {
        public static string ZoneName(this Operation operation) => new Uri(operation.Zone).Segments.Last();

        public static async Task NewWait(this Operation operation, ComputeService service, string projectId)
        {
            try
            {
                Debug.WriteLine($"Waiting on operation {operation.Name}");
                while (true)
                {
                    var newOperation = await service.ZoneOperations.Get(projectId, operation.ZoneName(), operation.Name).ExecuteAsync();
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
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to read operation: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }
    }
}
