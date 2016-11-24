// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Diagnostics;

namespace GoogleCloudExtension.Utils
{
    internal class CloudSqlInstanceConnection
    {
        public string Server { get; }

        public string Database { get; }

        public CloudSqlInstanceConnection(string server, string database)
        {
            Server = server;
            Database = database;
        }
    }

    internal static class CloudSqlUtils
    {
        /// <summary>
        /// The GUID for the Cloud SQL database.
        /// </summary>
        public static Guid DataSource { get; } = new Guid("{98FBE4D8-5583-4233-B219-70FF8C7FBBBD}");

        /// <summary>
        /// The GUID for the MySQL Database Provider.
        /// </summary>
        public static Guid DataProvider { get; } = new Guid("{C6882346-E592-4da5-80BA-D2EADCDA0359}");

        /// <summary>
        /// Formats a connection string for the given server.
        /// </summary>
        /// <param name="server">The server name or IP address.</param>
        public static string FormatServerConnectionString(string server) => $"server={server}";

        /// <summary>
        /// Parses the connection string into its portions.
        /// </summary>
        public static CloudSqlInstanceConnection ParseConnection(string connection)
        {
            var values = connection.Split(';');
            string server = null;
            string database = null;

            foreach (var value in values)
            {
                var parsedValue = value.Split('=');
                if (parsedValue.Length != 2)
                {
                    Debug.WriteLine($"Invalid value in connection string: {value}");
                    continue;
                }

                switch (parsedValue[0])
                {
                    case "server":
                        server = parsedValue[1];
                        break;

                    case "database":
                        database = parsedValue[1];
                        break;
                }
            }

            return new CloudSqlInstanceConnection(server: server, database: database);
        }
    }
}
