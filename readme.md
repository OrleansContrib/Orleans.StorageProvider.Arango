# Arango Storage Provider for Microsoft Orleans

Can be used to store grain state in an [ArangoDB](https://www.arangodb.com/) database.

## Installation

Nugets coming soon...

## Usage

Register the provider like this:

```c#
config.Globals.RegisterArangoStorageProvider("ARANGO",
    url: "http://localhost:8529",
    username: "root",
    password: "password");
```

Then from your grain code configure grain storage in the normal way:

```c#
// define a state interface
public interface IMyGrainState : IGrainState
{
        string Value { get; set; }
}

// Select the BlobStore as the storage provider for the grain
[StorageProvider(ProviderName="BlobStore")]
public class Grain1 : Orleans.Grain<IMyGrainState>, IGrain1
{
        public Task Test(string value)
        {
            // set the state and save it
                this.State.Value = value;
                return this.State.WriteStateAsync();
        }

}
```

Note:

* Grain state can be stored using a database name of your choice. The default is 'Orleans'.
* The state is stored in a collection called 'GrainState'.

## License

MIT
