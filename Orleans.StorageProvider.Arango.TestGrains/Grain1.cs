using Orleans.Providers;
using System;
using System.Threading.Tasks;

namespace Orleans.StorageProvider.Arango.TestGrains
{
    public interface IGrain1 : IGrainWithIntegerKey
    {
        Task Set(string stringValue, int intValue, DateTime dateTimeValue, Guid guidValue, IGrain1 grainValue);
        Task<Tuple<string, int, DateTime, Guid, IGrain1>> Get();
    }

    public class MyState 
    {
        public string StringValue { get; set; }
        public int IntValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public Guid GuidValue { get; set; }
        public IGrain1 GrainValue { get; set; }
    }

    [StorageProvider(ProviderName = "ARANGO")]
    public class Grain1 : Grain<MyState>, IGrain1
    {
        public Task Set(string stringValue, int intValue, DateTime dateTimeValue, Guid guidValue, IGrain1 grainValue)
        {
            State.StringValue = stringValue;
            State.IntValue = intValue;
            State.DateTimeValue = dateTimeValue;
            State.GuidValue = guidValue;
            State.GrainValue = grainValue;
            return WriteStateAsync();
        }

        public Task<Tuple<string, int, DateTime, Guid, IGrain1>> Get()
        {
            return Task.FromResult(new Tuple<string, int, DateTime, Guid, IGrain1>(
              State.StringValue,
              State.IntValue,
              State.DateTimeValue,
              State.GuidValue,
              State.GrainValue));
        }


    }
}
