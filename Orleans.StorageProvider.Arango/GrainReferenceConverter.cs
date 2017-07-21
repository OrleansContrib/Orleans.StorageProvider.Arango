using System;
using System.Net;
using Newtonsoft.Json;
using Orleans.Runtime;
using Orleans.Serialization;

namespace Orleans.StorageProvider.Arango
{
    internal class GrainReferenceInfo
    {
        public string Key { get; set; }

        public string Data { get; set; }
    }

    internal class GrainReferenceConverter : JsonConverter
    {
        public GrainReferenceConverter(SerializationManager serializationManager, IGrainFactory grainFactory)
        {

            serializerSettings = OrleansJsonSerializer.GetDefaultSerializerSettings(serializationManager, grainFactory);
        }

        JsonSerializerSettings serializerSettings;

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
                Data = JsonConvert.SerializeObject(value, serializerSettings)
            };
            serializer.Serialize(writer, info);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var info = new GrainReferenceInfo();

            serializer.Populate(reader, info);

            return JsonConvert.DeserializeObject(info.Data, serializerSettings);
        }

        public override bool CanConvert(Type objectType)
        {
            if (typeof(IGrain).IsAssignableFrom(objectType)) return true;
            if (objectType == typeof(IPAddress)) return true;
            if (objectType == typeof(IPEndPoint)) return true;

            return false;
        }
    }
}
