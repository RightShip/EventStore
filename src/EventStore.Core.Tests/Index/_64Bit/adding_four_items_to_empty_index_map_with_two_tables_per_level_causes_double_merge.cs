using NUnit.Framework;

namespace EventStore.Core.Tests.Index._64Bit
{
    [TestFixture]
    public class adding_four_items_to_empty_index_map_with_two_tables_per_level_causes_double_merge: _32Bit.adding_four_items_to_empty_index_map_with_two_tables_per_level_causes_double_merge
    {
        public adding_four_items_to_empty_index_map_with_two_tables_per_level_causes_double_merge()
        {
            ptableVersion = 2;
        }
    }
}