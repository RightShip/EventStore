using EventStore.Core.Index;
using NUnit.Framework;

namespace EventStore.Core.Tests.Index._64Bit
{
    [TestFixture]
    public class when_merging_ptables_with_entries_to_nonexisting_record: _32Bit.when_merging_ptables_with_entries_to_nonexisting_record
    {
        public when_merging_ptables_with_entries_to_nonexisting_record()
        {
            _ptableVersion = PTableVersions.Index64Bit;
        }
    }
}