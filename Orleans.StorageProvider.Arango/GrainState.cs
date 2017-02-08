using ArangoDB.Client;
using System;

namespace Orleans.StorageProvider.Arango
{
    [Serializable]
    class GrainState
    {
        [DocumentProperty(Identifier = IdentifierType.Revision)]
        public string Revision { get; set; }

        [DocumentProperty(Identifier = IdentifierType.Key)]
        public string Id { get; set; }

        public object State { get; set; }
    }
}
