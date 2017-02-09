using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.StorageProvider.Arango
{
    [Serializable]
    public class ArangoStorageException : Exception
    {
        public ArangoStorageException() : base ()
        { }

        public ArangoStorageException(string message) : base(message) { }

        public ArangoStorageException(SerializationInfo info, StreamingContext context) : base(info, context) { }


    }
}
