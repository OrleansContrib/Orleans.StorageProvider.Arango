using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ArangoDB.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.StorageProvider.Arango
{
    public class ArangoStorageProvider : IStorageProvider
    {
        public ArangoDatabase Database { get; private set; }
        public Logger Log { get; private set; }
        public string Name { get; private set; }


        static Newtonsoft.Json.JsonSerializer jsonSerializerSettings;
        string collectionName;

        ConcurrentBag<string> initialisedCollections = new ConcurrentBag<string>();

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
            collectionName = config.GetProperty("CollectionName", null);

            var grainRefConverter = new GrainReferenceConverter();

            ArangoDatabase.ChangeSetting(s =>
            {
                s.Database = databaseName;
                s.Url = url;
                s.Credential = new NetworkCredential(username, password);
                s.DisableChangeTracking = true;
                s.WaitForSync = waitForSync;
                s.Serialization.Converters.Add(grainRefConverter);
            });

            jsonSerializerSettings = new JsonSerializer();
            jsonSerializerSettings.Converters.Add(grainRefConverter);

            this.Database = new ArangoDatabase();
        }

        async Task<IDocumentCollection> InitialiseCollection(string name)
        {
            if (!this.initialisedCollections.Contains(name))
            {
                try
                {
                    await this.Database.CreateCollectionAsync(name);
                }
                catch (Exception ex)
                {
                    this.Log.Info($"Arango Storage Provider: Error creating {name} collection, it may already exist");
                }

                this.initialisedCollections.Add(name);
            }

            return this.Database.Collection(name);
        }

        Task<IDocumentCollection> GetCollection(string grainType)
        {
            if (!string.IsNullOrWhiteSpace(this.collectionName))
            {
                return InitialiseCollection(this.collectionName);
            }

            return InitialiseCollection(grainType.Split('.').Last().ToArangoCollectionName());
        }

        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            try
            {
                var primaryKey = grainReference.ToArangoKeyString();
                var collection = await GetCollection(grainType);
                
                var result = await collection.DocumentAsync<GrainState>(primaryKey).ConfigureAwait(false);
                if (null == result) return;

                if (result.State != null)
                {
                    grainState.State = (result.State as JObject).ToObject(grainState.State.GetType(), jsonSerializerSettings);
                }
                else
                {
                    grainState.State = null;
                }
                grainState.ETag = result.Revision;
            }
            catch (Exception ex)
            {
                this.Log.Error(190000, "ArangoStorageProvider.ReadStateAsync()", ex);
                throw new ArangoStorageException(ex.ToString());
            }
        }

        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            try
            {
                var primaryKey = grainReference.ToArangoKeyString();
                var collection = await GetCollection(grainType);

                var document = new GrainState
                {
                    Id = primaryKey,
                    Revision = grainState.ETag,
                    State = grainState.State
                };

                if (string.IsNullOrWhiteSpace(grainState.ETag))
                {
                    var result = await collection.InsertAsync(document).ConfigureAwait(false);
                    grainState.ETag = result.Rev;
                }
                else
                {
                    var result = await collection.UpdateByIdAsync(primaryKey, document).ConfigureAwait(false);
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
                var primaryKey = grainReference.ToArangoKeyString();
                var collection = await GetCollection(grainType);

                await collection.RemoveByIdAsync(primaryKey).ConfigureAwait(false);

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
