using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Orleans.Runtime;
using Orleans.Serialization;

namespace Orleans.StorageProvider.Arango
{
    internal class GrainReferenceInfo
    {
        public string Key { get; set; }

        public byte[] Data { get; set; }
    }

    internal class GrainReferenceConverter : JsonConverter
    {
        public override bool CanRead
        {
            get { return true; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var reference = (GrainReference)value;
            string key = reference.ToKeyString();
            var info = new GrainReferenceInfo
            {
                Key = key,
                Data = SerializationManager.SerializeToByteArray(value)
            };
            serializer.Serialize(writer, info);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var info = new GrainReferenceInfo();
            serializer.Populate(reader, info);
            return SerializationManager.Deserialize(objectType, new BinaryTokenStreamReader(info.Data));
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(IGrain).IsAssignableFrom(objectType);
        }
    }
}
