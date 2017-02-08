using Orleans.Runtime.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.StorageProvider.Arango
{
    public static class ExtensionMethods
    {
        public static void RegisterArangoStorageProvider(
            this GlobalConfiguration globalConfig, 
            string name,
            string databaseName = "Orleans",
            string url = "http://localhost:8529",
            string username = "root",
            string password = "password",
            bool waitForSync = true)
        {
            var properties = new Dictionary<string, string>();

            properties.Add("DatabaseName", databaseName);
            properties.Add("Url", url);
            properties.Add("Username", username);
            properties.Add("Password", password);
            properties.Add("WaitForSync", waitForSync.ToString());

            globalConfig.RegisterStorageProvider<ArangoStorageProvider>(name, properties);

        }
    }
}
