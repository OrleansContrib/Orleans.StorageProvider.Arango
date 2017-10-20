using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orleans.StorageProvider.Arango.TestGrains;
using Orleans.TestingHost;

namespace Orleans.StorageProvider.Arango.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task TestGrains()
        {

            // insert your grain test code here
            var grain = _cluster.GrainFactory.GetGrain<IGrain1>("1234");
            var now = DateTime.UtcNow;
            var guid = Guid.NewGuid();
            await grain.Set("string value", 12344, now, guid, grain);
            await grain.Set("string value", 12345, now, guid, grain);

            var result = await grain.Get();
            Assert.AreEqual("string value", result.Item1);
            Assert.AreEqual(12345, result.Item2);
            Assert.AreEqual(now, result.Item3);
            Assert.AreEqual(guid, result.Item4);
            Assert.AreEqual("1234", result.Item5.GetPrimaryKeyString());

            await grain.Clear();
        }

        /*
        [TestMethod]
        public void TestKeys()
        {
            var grainRef = GrainReference.FromKeyString(@"GrainReference=000000000000000000000000000000000600000040155719+This£is#a#bad document~key!");
            var key = grainRef.ToArangoKeyString();
            Assert.AreEqual("GrainReference=000000000000000000000000000000000600000040155719_This_is_a_bad_document_key!", key);
        }
        */

        // code to initialize and clean up an Orleans Silo

        static TestCluster _cluster;

        [ClassInitialize]
        public static void Setup(TestContext context)
        {
            _cluster = new TestCluster();
            _cluster.ClusterConfiguration.Globals.RegisterArangoStorageProvider("ARANGO");
            _cluster.Deploy();
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            _cluster.StopAllSilos();
        }
    }
}
