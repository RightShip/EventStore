using EventStore.Core.Index;
using NUnit.Framework;

namespace EventStore.Core.Tests.Index._32Bit
{
    public abstract class PTableReadScenario: SpecificationWithFile
    {
        private readonly int _midpointCacheDepth;
        protected int _ptableVersion = PTableVersions.Index32Bit;

        protected PTable PTable;

        protected PTableReadScenario(int midpointCacheDepth)
        {
            _midpointCacheDepth = midpointCacheDepth;
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            var table = new HashListMemTable(maxSize: 50);

            AddItemsForScenario(table);

            PTable = PTable.FromMemtable(table, Filename, _ptableVersion, cacheDepth: _midpointCacheDepth);
        }

        [TearDown]
        public override void TearDown()
        {
            PTable.Dispose();
            
            base.TearDown();
        }

        protected abstract void AddItemsForScenario(IMemTable memTable);
    }
}