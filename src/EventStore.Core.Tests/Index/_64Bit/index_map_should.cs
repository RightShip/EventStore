using EventStore.Core.Index;
using NUnit.Framework;

namespace EventStore.Core.Tests.Index._64Bit
{
    [TestFixture]
    public class index_map_should: _32Bit.index_map_should
    {
        public index_map_should()
        {
            _ptableVersion = PTableVersions.Index64Bit;
        }
    }
}