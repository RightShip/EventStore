using EventStore.Core.Index;
using NUnit.Framework;

namespace EventStore.Core.Tests.Index.Index64Bit
{
    [TestFixture, Explicit]
    public class opening_a_ptable_with_more_thanIndex64Bits_of_records: Index32Bit.opening_a_ptable_with_more_thanIndex32Bits_of_records
    {
        public opening_a_ptable_with_more_thanIndex64Bits_of_records()
        {
            _ptableVersion = PTableVersions.Index64Bit;
            indexEntrySize = PTable.IndexEntry64Size;
        }
    }
}