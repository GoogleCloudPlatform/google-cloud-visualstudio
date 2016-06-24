using MySql.Data.MySqlClient;
using System;

namespace GoogleCloudExtension.Utils
{
    internal static class MySQLUtils
    {
        /// <summary>
        /// The GUID for the MySQL Database.
        /// </summary>
        public static readonly Guid MySQLDataSource = new Guid("{98FBE4D8-5583-4233-B219-70FF8C7FBBBD}");

        /// <summary>
        /// The GUID for the MySQL Database Provider.
        /// </summary>
        public static readonly Guid MySQLDataProvider = new Guid("{C6882346-E592-4da5-80BA-D2EADCDA0359}");
    }
}
