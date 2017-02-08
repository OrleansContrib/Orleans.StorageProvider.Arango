using Orleans.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Providers;
using Orleans.Runtime;
using ArangoDB.Client;
using System.Net;
using Newtonsoft.Json;

namespace Orleans.StorageProvider.Arango
{
    public class ArangoStorageProvider : IStorageProvider
    {
        public ArangoDatabase Database { get; private set; }
        public Logger Log { get; private set; }
        public string Name { get; private set; }

        public Task Close()
        {
            this.Database.Dispose();
            return TaskDone.Done;
        }

        public async Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            this.Log = providerRuntime.GetLogger(nameof(ArangoStorageProvider));
            this.Name = name;

            var databaseName = config.GetProperty("DatabaseName", "Orleans");
            var url = config.GetProperty("Url", "http://localhost:8529");
            var username = config.GetProperty("Username", "root");
            var password = config.GetProperty("Password", "password");
            var waitForSync = config.GetBoolProperty("WaitForSync", true);

            // the arango DB driver assumes that tables are names after the entity types
            var collectionName = nameof(GrainState); 

            ArangoDatabase.ChangeSetting(s =>
            {
                s.Database = databaseName;
                s.Url = url;
                s.Credential = new NetworkCredential(username, password);
                s.DisableChangeTracking = true;
                s.WaitForSync = waitForSync;
            });

            this.Database = new ArangoDatabase();

            try
            {
                await this.Database.CreateCollectionAsync(collectionName);
            }
            catch (Exception ex)
            {
                this.Log.Info($"Arango Storage Provider: Error creating {collectionName} collection, it may already exist");
            }
        }

        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            try
            {
                var primaryKey = grainReference.ToKeyString();

                var result = await this.Database.DocumentAsync<GrainState>(primaryKey).ConfigureAwait(false);
                if (null == result)
                {
                    return;
                }
                grainState.State = result.State;
                grainState.ETag = result.Revision;
            }
            catch (Exception ex)
            {
                this.Log.Error(190000, "ArangoStorageProvider.ClearStateAsync()", ex);
                throw new ArangoStorageException(ex.ToString());
            }
        }

        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            try
            {
                var primaryKey = grainReference.ToKeyString();

                var document = new GrainState
                {
                    Id = primaryKey,
                    Revision = grainState.ETag,
                    State = grainState.State
                };

                if (string.IsNullOrWhiteSpace(grainState.ETag))
                {
                    var result = await this.Database.InsertAsync<GrainState>(document).ConfigureAwait(false);
                    grainState.ETag = result.Rev;
                }
                else
                {
                    var result = await this.Database.UpdateAsync<GrainState>(document).ConfigureAwait(false);
                    grainState.ETag = result.Rev;
                }
            }
            catch (Exception ex)
            {
                this.Log.Error(190001, "ArangoStorageProvider.WriteStateAsync()", ex);
                throw new ArangoStorageException(ex.ToString());
            }
        }

        public async Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            try
            {
                var primaryKey = grainReference.ToKeyString();

                await this.Database.RemoveByIdAsync<GrainState>(primaryKey).ConfigureAwait(false);

                grainState.ETag = null;
            }
            catch (Exception ex)
            {
                this.Log.Error(190002, "ArangoStorageProvider.ClearStateAsync()", ex);
                throw new ArangoStorageException(ex.ToString());
            }
        }
    }
}
