using System.Linq;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Text.RegularExpressions;
using $safeprojectname$.Models;

namespace $safeprojectname$
{
    public class MySqlInitializer : IDatabaseInitializer<ApplicationDbContext>
    {
        public void InitializeDatabase(ApplicationDbContext context)
        {
            if (!context.Database.Exists())
            {
                // Create database if it does not exist
                context.Database.Create();
            }
            else
            {
                // Read database name from configured connection string in Web.config
                var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                var matchDatabaseName = Regex.Match(connectionString, ";Database=([^;]+)");

                if (matchDatabaseName.Success)
                {
                    var databaseName = matchDatabaseName.Groups[1].Value;

                    // Query to check if MigrationHistory table is present in the database
                    var migrationHistoryTableExists = ((IObjectContextAdapter)context).ObjectContext.ExecuteStoreQuery<int>(
                    string.Format(
                      "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '{0}' AND table_name = '__MigrationHistory'",
                      databaseName));

                    // Create MigrationHistory table if it does not exist
                    if (migrationHistoryTableExists.FirstOrDefault() == 0)
                    {
                        context.Database.Delete();
                        context.Database.Create();
                    }
                }
            }
        }
    }
}