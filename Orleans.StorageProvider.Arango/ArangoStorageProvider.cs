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
using Newtonsoft.Json.Linq;

namespace Orleans.StorageProvider.Arango
{
    public class ArangoStorageProvider : IStorageProvider
    {
        public ArangoDatabase Database { get; private set; }
        public Logger Log { get; private set; }
        public string Name { get; private set; }
        private List<ArangoDB.Client.Data.CreateCollectionResult> collectionsList;
        private bool waitForSync;
        private JsonSerializerSettings settings;

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
            waitForSync = config.GetBoolProperty("WaitForSync", true);
            settings = Orleans.Serialization.OrleansJsonSerializer.GetDefaultSerializerSettings();
            settings.DefaultValueHandling = DefaultValueHandling.Include;

            ArangoDatabase.ChangeSetting(s =>
            {
                s.Database = databaseName;
                s.Url = url;
                s.Credential = new NetworkCredential(username, password);
                s.DisableChangeTracking = true;
                s.WaitForSync = waitForSync;
            });

            this.Database = new ArangoDatabase();
            collectionsList = await this.Database.ListCollectionsAsync();
        }

        private async Task CreateCollectionIfNeeded(bool waitForSync, string collectionName)
        {
            try
            {

                if (!collectionsList.Any(x => x.Name == collectionName))
                {
                    var addedCollection = await this.Database.CreateCollectionAsync(collectionName, waitForSync: waitForSync);
                    collectionsList.Add(addedCollection);
                }
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
                string collectionName = grainType.Replace(".", "-");
                await CreateCollectionIfNeeded(this.waitForSync, collectionName);
                var primaryKey = grainReference.ToKeyString();
                primaryKey = primaryKey.Replace("GrainReference=", "GR:").Replace("+", ":");
                if (Log.IsVerbose)
                    Log.Info("reading {0}", primaryKey);


                var result = await this.Database.Collection(collectionName).DocumentAsync<GrainState>(primaryKey).ConfigureAwait(false);
                Log.Info("is it null {0}", result == null);
                if (null == result)
                {
                    return;
                }

                if (result.State != null)
                {
                    grainState.State = JsonConvert.DeserializeObject((result.State as string), grainState.State.GetType(), settings);
                }
                else
                {
                    grainState.State = null;
                }
                Log.Info("Reading result {0} eTag:{1}", result.Id, result.Revision);
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
                string collectionName = grainType.Replace(".", "-");
                var primaryKey = grainReference.ToKeyString();
                primaryKey = primaryKey.Replace("GrainReference=", "GR:").Replace("+", ":");
                var document = new GrainState
                {
                    Id = primaryKey,
                    Revision = grainState.ETag,
                    State = JsonConvert.SerializeObject(grainState.State, grainState.State.GetType(), settings)
                };
                if (Log.IsVerbose)
                    Log.Info("writing {0} with type {1} and eTag {2}", primaryKey, grainType, grainState.ETag);
                await CreateCollectionIfNeeded(this.waitForSync, collectionName);
                if (string.IsNullOrWhiteSpace(grainState.ETag))
                {
                    var result = await this.Database.Collection(collectionName).InsertAsync(document).ConfigureAwait(false);
                    grainState.ETag = result.Rev;
                }
                else
                {
                    var result = await this.Database.Collection(collectionName).UpdateByIdAsync(primaryKey, document).ConfigureAwait(false);
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
                string collectionName = grainType.Replace(".", "-");
                await CreateCollectionIfNeeded(this.waitForSync, collectionName);
                var primaryKey = grainReference.ToKeyString();
                primaryKey = primaryKey.Replace("GrainReference=", "GR:").Replace("+", ":");
                await this.Database.Collection(collectionName).RemoveByIdAsync(primaryKey).ConfigureAwait(false);
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
