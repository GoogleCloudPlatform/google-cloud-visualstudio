// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.DataSources.Models;
using System.Collections.Generic;
using System.Linq;

namespace GoogleCloudExtension.DataSources
{
    public static class MetadataExtensions
    {
        /// <summary>
        /// Returns a new list of metadata entries in which the key/value pair has been stored. If
        /// the <c>entries</c> already has an entry for key it is overwritten.
        /// </summary>
        /// <param name="entries">The source list of <c>MetadataEntry</c>.</param>
        /// <param name="key">The key to set.</param>
        /// <param name="value">The value for the key.</param>
        /// <returns></returns>
        public static IList<MetadataEntry> SetValue(this IList<MetadataEntry> entries, string key, string value)
        {
            var result = entries.ToDictionary(x => x.Key, y => y.Value);
            result[key] = value;
            return result.Select(x => new MetadataEntry { Key = x.Key, Value = x.Value }).ToList();
        }
    }
}
