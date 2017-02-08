using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.StorageProvider.Arango
{
    [Serializable]
    public class ArangoStorageException : Exception
    {
        public ArangoStorageException()
        { }

        public ArangoStorageException(string message) : base(message) { }
      
    }
}
