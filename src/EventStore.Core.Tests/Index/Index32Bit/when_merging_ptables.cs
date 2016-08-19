using System;
using System.Collections.Generic;
using EventStore.Core.Index;
using NUnit.Framework;

namespace EventStore.Core.Tests.Index.Index32Bit
{
    [TestFixture]
    public class when_merging_ptables : SpecificationWithDirectoryPerTestFixture
    {
        private readonly List<string> _files = new List<string>();
        private readonly List<PTable> _tables = new List<PTable>();

        private PTable _newtable;

        [TestFixtureSetUp]
        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();
            _files.Add(GetTempFilePath());
            var table = new HashListMemTable(maxSize: 20);
            table.Add(1, 0, 1);
            table.Add(2, 0, 2);
            table.Add(3, 0, 3);
            table.Add(4, 0, 4);
            _tables.Add(PTable.FromMemtable(table, GetTempFilePath(), PTableVersions.Index32Bit));
            table = new HashListMemTable(maxSize: 20);
            table.Add(5, 0, 5);
            table.Add(6, 0, 6);
            table.Add(7, 0, 7);
            table.Add(8, 0, 8);
            _tables.Add(PTable.FromMemtable(table, GetTempFilePath(), PTableVersions.Index32Bit));
            _newtable = PTable.MergeTo(_tables, GetTempFilePath(), (streamId, hash) => hash + 1, x => new Tuple<string, bool>(x.Stream.ToString(), true), PTableVersions.Index32Bit);
        }

        [TestFixtureTearDown]
        public override void TestFixtureTearDown()
        {
            _newtable.Dispose();
            foreach (var ssTable in _tables)
            {
                ssTable.Dispose();
            }
            base.TestFixtureTearDown();
        }

        [Test]
        public void merged_ptable_is_32bit()
        {
            Assert.AreEqual(PTableVersions.Index32Bit, _newtable.Version);
        }

        [Test]
        public void there_are_8_records_in_the_merged_index()
        {
            Assert.AreEqual(8, _newtable.Count);
        }

        [Test]
        public void no_entries_should_have_upgraded_hashes()
        {
            foreach (var item in _newtable.IterateAllInOrder())
            {
                Assert.IsTrue((ulong)item.Position == item.Stream, "Expected the Stream (Hash) {0} to be equal to {1}", item.Stream, item.Position);
            }
        }
    }

    [TestFixture]
    public class when_merging_ptables_to_64bit: SpecificationWithDirectoryPerTestFixture
    {
        private readonly List<string> _files = new List<string>();
        private readonly List<PTable> _tables = new List<PTable>();

        private PTable _newtable;

        [TestFixtureSetUp]
        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();
            _files.Add(GetTempFilePath());
            var table = new HashListMemTable(maxSize: 20);
            table.Add(1, 0, 1);
            table.Add(2, 0, 2);
            table.Add(3, 0, 3);
            table.Add(4, 0, 4);
            _tables.Add(PTable.FromMemtable(table, GetTempFilePath(), PTableVersions.Index32Bit));
            table = new HashListMemTable(maxSize: 20);
            table.Add(5, 0, 5);
            table.Add(6, 0, 6);
            table.Add(7, 0, 7);
            table.Add(8, 0, 8);
            _tables.Add(PTable.FromMemtable(table, GetTempFilePath(), PTableVersions.Index32Bit));
            _newtable = PTable.MergeTo(_tables, GetTempFilePath(), (streamId, hash) => hash + 1, x => new Tuple<string, bool>(x.Stream.ToString(), true), PTableVersions.Index64Bit);
        }

        [TestFixtureTearDown]
        public override void TestFixtureTearDown()
        {
            _newtable.Dispose();
            foreach (var ssTable in _tables)
            {
                ssTable.Dispose();
            }
            base.TestFixtureTearDown();
        }

        [Test]
        public void merged_ptable_is_64bit()
        {
            Assert.AreEqual(PTableVersions.Index64Bit, _newtable.Version);
        }

        [Test]
        public void there_are_8_records_in_the_merged_index()
        {
            Assert.AreEqual(8, _newtable.Count);
        }

        [Test]
        public void all_the_entries_have_upgraded_hashes()
        {
            foreach (var item in _newtable.IterateAllInOrder())
            {
                Assert.IsTrue((ulong)item.Position == item.Stream - 1, "Expected the Stream (Hash) {0} to be equal to {1}", item.Stream - 1, item.Position);
            }
        }
    }

    [TestFixture]
    public class when_merging_2_32bit_ptables_and_1_64bit_ptable_to_64bit : SpecificationWithDirectoryPerTestFixture
    {
        private readonly List<string> _files = new List<string>();
        private readonly List<PTable> _tables = new List<PTable>();

        private PTable _newtable;

        [TestFixtureSetUp]
        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();
            _files.Add(GetTempFilePath());
            var table = new HashListMemTable(maxSize: 20);
            table.Add(1, 0, 1);
            table.Add(2, 0, 2);
            table.Add(3, 0, 3);
            table.Add(4, 0, 4);
            _tables.Add(PTable.FromMemtable(table, GetTempFilePath(), PTableVersions.Index32Bit));
            table = new HashListMemTable(maxSize: 20);
            table.Add(5, 0, 5);
            table.Add(6, 0, 6);
            table.Add(7, 0, 7);
            table.Add(8, 0, 8);
            _tables.Add(PTable.FromMemtable(table, GetTempFilePath(), PTableVersions.Index32Bit));
            table = new HashListMemTable(maxSize: 20);
            table.Add(9, 0, 9);
            table.Add(10, 0, 10);
            table.Add(11, 0, 11);
            table.Add(12, 0, 12);
            _tables.Add(PTable.FromMemtable(table, GetTempFilePath(), PTableVersions.Index64Bit));
            _newtable = PTable.MergeTo(_tables, GetTempFilePath(), (streamId, hash) => hash + 1, x => new Tuple<string, bool>(x.Stream.ToString(), true), PTableVersions.Index64Bit);
        }

        [TestFixtureTearDown]
        public override void TestFixtureTearDown()
        {
            _newtable.Dispose();
            foreach (var ssTable in _tables)
            {
                ssTable.Dispose();
            }
            base.TestFixtureTearDown();
        }

        [Test]
        public void merged_ptable_is_64bit()
        {
            Assert.AreEqual(PTableVersions.Index64Bit, _newtable.Version);
        }

        [Test]
        public void there_are_12_records_in_the_merged_index()
        {
            Assert.AreEqual(12, _newtable.Count);
        }

        [Test]
        public void only_the_32_bit_index_entries_should_have_upgraded_hashes()
        {
            foreach (var item in _newtable.IterateAllInOrder())
            {
                if(item.Position >= 9) //these are 64bit already
                {
                    Assert.IsTrue((ulong)item.Position == item.Stream, "Expected the Stream (Hash) {0} to be equal to {1}", item.Stream, item.Position);
                }
                else
                {
                    Assert.IsTrue((ulong)item.Position == item.Stream - 1, "Expected the Stream (Hash) {0} to be equal to {1}", item.Stream - 1, item.Position);
                }
            }
        }
    }

    [TestFixture]
    public class when_merging_1_32bit_ptables_and_1_64bit_ptable_with_missing_entries_to_64bit : SpecificationWithDirectoryPerTestFixture
    {
        private readonly List<string> _files = new List<string>();
        private readonly List<PTable> _tables = new List<PTable>();

        private PTable _newtable;

        [TestFixtureSetUp]
        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();
            _files.Add(GetTempFilePath());
            var table = new HashListMemTable(maxSize: 20);
            table.Add(1, 2, 5);
            table.Add(2, 1, 6);
            table.Add(2, 2, 7);
            table.Add(4, 0, 8);
            table.Add(4, 1, 9);
            _tables.Add(PTable.FromMemtable(table, GetTempFilePath(), PTableVersions.Index64Bit));
            table = new HashListMemTable(maxSize: 20);
            table.Add(1, 1, 10);
            table.Add(1, 2, 11);
            table.Add(5, 1, 12);
            table.Add(5, 2, 13);
            table.Add(5, 3, 14);
            _tables.Add(PTable.FromMemtable(table, GetTempFilePath(), PTableVersions.Index32Bit));
            _newtable = PTable.MergeTo(_tables, GetTempFilePath(), (streamId, hash) => hash + 1, x => new Tuple<string, bool>(x.Stream.ToString(), x.Position % 2 == 0), PTableVersions.Index64Bit);
        }

        [TestFixtureTearDown]
        public override void TestFixtureTearDown()
        {
            _newtable.Dispose();
            foreach (var ssTable in _tables)
            {
                ssTable.Dispose();
            }
            base.TestFixtureTearDown();
        }

        [Test]
        public void merged_ptable_is_64bit()
        {
            Assert.AreEqual(PTableVersions.Index64Bit, _newtable.Version);
        }

        [Test]
        public void there_are_5_records_in_the_merged_index()
        {
            Assert.AreEqual(5, _newtable.Count);
        }

        [Test]
        public void the_items_are_sorted()
        {
            var last = new IndexEntry(ulong.MaxValue, long.MaxValue);
            foreach (var item in _newtable.IterateAllInOrder())
            {
                Assert.IsTrue(last.Key > item.Key || last.Key == item.Key && last.Position > item.Position);
                last = item;
            }
        }
    }

    [TestFixture]
    public class when_merging_2_32bit_ptables_and_1_64bit_ptable_with_missing_entries_to_64bit : SpecificationWithDirectoryPerTestFixture
    {
        private readonly List<string> _files = new List<string>();
        private readonly List<PTable> _tables = new List<PTable>();

        private PTable _newtable;

        [TestFixtureSetUp]
        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();
            _files.Add(GetTempFilePath());
            var table = new HashListMemTable(maxSize: 20);
            table.Add(1, 0, 1);
            table.Add(2, 0, 2);
            table.Add(3, 0, 3);
            table.Add(3, 1, 4);
            _tables.Add(PTable.FromMemtable(table, GetTempFilePath(), PTableVersions.Index32Bit));
            table = new HashListMemTable(maxSize: 20);
            table.Add(1, 2, 5);
            table.Add(2, 1, 6);
            table.Add(2, 2, 7);
            table.Add(4, 0, 8);
            table.Add(4, 1, 9);
            _tables.Add(PTable.FromMemtable(table, GetTempFilePath(), PTableVersions.Index64Bit));
            table = new HashListMemTable(maxSize: 20);
            table.Add(1, 1, 10);
            table.Add(1, 2, 11);
            table.Add(5, 1, 12);
            table.Add(5, 2, 13);
            table.Add(5, 3, 14);
            _tables.Add(PTable.FromMemtable(table, GetTempFilePath(), PTableVersions.Index32Bit));
            _newtable = PTable.MergeTo(_tables, GetTempFilePath(), (streamId, hash) => hash + 1, x => new Tuple<string, bool>(x.Stream.ToString(), x.Position % 2 == 0), PTableVersions.Index64Bit);
        }

        [TestFixtureTearDown]
        public override void TestFixtureTearDown()
        {
            _newtable.Dispose();
            foreach (var ssTable in _tables)
            {
                ssTable.Dispose();
            }
            base.TestFixtureTearDown();
        }

        [Test]
        public void merged_ptable_is_64bit()
        {
            Assert.AreEqual(PTableVersions.Index64Bit, _newtable.Version);
        }

        [Test]
        public void there_are_7_records_in_the_merged_index()
        {
            Assert.AreEqual(7, _newtable.Count);
        }

        [Test]
        public void the_items_are_sorted()
        {
            var last = new IndexEntry(ulong.MaxValue, long.MaxValue);
            foreach (var item in _newtable.IterateAllInOrder())
            {
                Assert.IsTrue(last.Key > item.Key || last.Key == item.Key && last.Position > item.Position);
                last = item;
            }
        }
    }
}