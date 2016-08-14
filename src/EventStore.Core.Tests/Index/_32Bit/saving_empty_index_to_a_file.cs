using System;
using System.IO;
using System.Linq;
using EventStore.Core.Index;
using EventStore.Core.Util;
using NUnit.Framework;

namespace EventStore.Core.Tests.Index._32Bit
{
    [TestFixture]
    public class saving_empty_index_to_a_file: SpecificationWithDirectoryPerTestFixture
    {
        private string _filename;
        private IndexMap _map;
        protected int ptableVersion = PTableVersions.Index32Bit;

        [TestFixtureSetUp]
        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();
            
            _filename = GetFilePathFor("indexfile");
            _map = IndexMap.FromFile(_filename, ptableVersion);
            _map.SaveToFile(_filename);
        }

        [Test]
        public void the_file_exists()
        {
            Assert.IsTrue(File.Exists(_filename));
        }

        [Test]
        public void the_file_contains_correct_data()
        {
            using (var fs = File.OpenRead(_filename))
            using (var reader = new StreamReader(fs))
            {
                var text = reader.ReadToEnd();
                var lines = text.Replace("\r", "").Split('\n');

                fs.Position = 32;
                var md5 = MD5Hash.GetHashFor(fs);
                var md5String = BitConverter.ToString(md5).Replace("-", "");

                Assert.AreEqual(4, lines.Count());
                Assert.AreEqual(md5String, lines[0]);
                Assert.AreEqual(ptableVersion.ToString(), lines[1]);
                Assert.AreEqual("-1/-1", lines[2]);
                Assert.AreEqual("", lines[3]);
            }
        }

        [Test]
        public void saved_file_could_be_read_correctly_and_without_errors()
        {
            var map = IndexMap.FromFile(_filename, ptableVersion);

            Assert.AreEqual(-1, map.PrepareCheckpoint);
            Assert.AreEqual(-1, map.CommitCheckpoint);
        }
    }
}