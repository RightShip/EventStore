using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using EventStore.Common.Utils;

namespace EventStore.Core.Index
{
    public unsafe partial class PTable
    {
        public static PTable FromFile(string filename, int version, int cacheDepth)
        {
            return new PTable(filename, Guid.NewGuid(), version, depth: cacheDepth);
        }

        public static PTable FromMemtable(IMemTable table, string filename, int version, int cacheDepth = 16)
        {
            Ensure.NotNull(table, "table");
            Ensure.NotNullOrEmpty(filename, "filename");
            Ensure.Nonnegative(cacheDepth, "cacheDepth");

            var indexEntrySize = version == PTableVersions.Index32Bit ? PTable.IndexEntry32Size : PTable.IndexEntry64Size;

            //Log.Trace("Started dumping MemTable [{0}] into PTable...", table.Id);
            var sw = Stopwatch.StartNew();
            using (var fs = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.None,
                                           DefaultSequentialBufferSize, FileOptions.SequentialScan))
            {
                fs.SetLength(PTableHeader.Size + indexEntrySize * (long)table.Count + MD5Size); // EXACT SIZE
                fs.Seek(0, SeekOrigin.Begin);

                using (var md5 = MD5.Create())
                using (var cs = new CryptoStream(fs, md5, CryptoStreamMode.Write))
                using (var bs = new BufferedStream(cs, DefaultSequentialBufferSize))
                {
                    // WRITE HEADER
                    var headerBytes = new PTableHeader((byte)version).AsByteArray();
                    cs.Write(headerBytes, 0, headerBytes.Length);

                    // WRITE INDEX ENTRIES
                    var buffer = new byte[indexEntrySize];
                    foreach (var record in table.IterateAllInOrder())
                    {
                        var rec = record;
                        AppendRecordTo(bs, buffer, version, rec, indexEntrySize);
                    }
                    bs.Flush();
                    cs.FlushFinalBlock();

                    // WRITE MD5
                    var hash = md5.Hash;
                    fs.Write(hash, 0, hash.Length);
                }
            }
            Log.Trace("Dumped MemTable [{0}, {1} entries] in {2}.", table.Id, table.Count, sw.Elapsed);
            return new PTable(filename, table.Id, depth: cacheDepth);
        }

        public static PTable MergeTo(IList<PTable> tables, string outputFile, Func<string, ulong, ulong> upgradeHash, Func<IndexEntry, Tuple<string, bool>> readRecord, int version, int cacheDepth = 16)
        {
            Ensure.NotNull(tables, "tables");
            Ensure.NotNullOrEmpty(outputFile, "outputFile");
            Ensure.Nonnegative(cacheDepth, "cacheDepth");

            var indexEntrySize = version == PTableVersions.Index32Bit ? PTable.IndexEntry32Size : IndexEntry64Size;

            var fileSize = GetFileSize(tables, indexEntrySize); // approximate file size
            if (tables.Count == 2)
                return MergeTo2(tables, fileSize, indexEntrySize, outputFile, upgradeHash, readRecord, version, cacheDepth); // special case

            Log.Trace("PTables merge started.");
            var watch = Stopwatch.StartNew();

            var enumerators = tables.Select(table => new EnumerablePTable(table, table.IterateAllInOrder().GetEnumerator())).ToList();
            for (int i = 0; i < enumerators.Count; i++)
            {
                if (!enumerators[i].MoveNext())
                {
                    enumerators[i].Dispose();
                    enumerators.RemoveAt(i);
                    i--;
                }
            }

            long dumpedEntryCount = 0;
            using (var f = new FileStream(outputFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None,
                                          DefaultSequentialBufferSize, FileOptions.SequentialScan))
            {
                f.SetLength(fileSize);
                f.Seek(0, SeekOrigin.Begin);

                using (var md5 = MD5.Create())
                using (var cs = new CryptoStream(f, md5, CryptoStreamMode.Write))
                using (var bs = new BufferedStream(cs, DefaultSequentialBufferSize))
                {
                    // WRITE HEADER
                    var headerBytes = new PTableHeader((byte)version).AsByteArray();
                    cs.Write(headerBytes, 0, headerBytes.Length);

                    var buffer = new byte[indexEntrySize];
                    // WRITE INDEX ENTRIES
                    while (enumerators.Count > 0)
                    {
                        //GetMaxOf:
                        //Compare 64 stream hashes
                        //A read and then if the entry exists perform an upgrade of the hash to 64bit
                        var idx = GetMaxOf(enumerators, version, upgradeHash, readRecord);
                        var current = enumerators[idx].Current;
                        var item = readRecord(current); //Possibly doing another read if the entry was read in GetMaxOf
                        if (item.Item2)
                        {
                            if (version == PTableVersions.Index64Bit && enumerators[idx].Table.Version == PTableVersions.Index32Bit)
                            {
                                current.Stream = upgradeHash(item.Item1, current.Stream);
                            }
                            AppendRecordTo(bs, buffer, version, current, indexEntrySize);
                            dumpedEntryCount += 1;
                        }
                        if (!enumerators[idx].MoveNext())
                        {
                            enumerators[idx].Dispose();
                            enumerators.RemoveAt(idx);
                        }
                    }
                    bs.Flush();
                    cs.FlushFinalBlock();

                    f.FlushToDisk();
                    f.SetLength(f.Position + MD5Size);

                    // WRITE MD5
                    var hash = md5.Hash;
                    f.Write(hash, 0, hash.Length);
                    f.FlushToDisk();
                }
            }
            Log.Trace("PTables merge finished in {0} ([{1}] entries merged into {2}).",
                      watch.Elapsed, string.Join(", ", tables.Select(x => x.Count)), dumpedEntryCount);
            return new PTable(outputFile, Guid.NewGuid(), depth: cacheDepth);
        }

        private static PTable MergeTo2(IList<PTable> tables, long fileSize, int indexEntrySize, string outputFile,
                                       Func<string, ulong, ulong> upgradeHash, Func<IndexEntry, Tuple<string, bool>> readRecord, 
                                       int version, int cacheDepth)
        {
            Log.Trace("PTables merge started (specialized for <= 2 tables).");
            var watch = Stopwatch.StartNew();

            var enumerators = tables.Select(table => new EnumerablePTable(table, table.IterateAllInOrder().GetEnumerator())).ToList();
            long dumpedEntryCount = 0;
            using (var f = new FileStream(outputFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None,
                                          DefaultSequentialBufferSize, FileOptions.SequentialScan))
            {
                f.SetLength(fileSize);
                f.Seek(0, SeekOrigin.Begin);

                using (var md5 = MD5.Create())
                using (var cs = new CryptoStream(f, md5, CryptoStreamMode.Write))
                using (var bs = new BufferedStream(cs, DefaultSequentialBufferSize))
                {
                    // WRITE HEADER
                    var headerBytes = new PTableHeader((byte)version).AsByteArray();
                    cs.Write(headerBytes, 0, headerBytes.Length);

                    // WRITE INDEX ENTRIES
                    var buffer = new byte[indexEntrySize];
                    var enum1 = enumerators[0];
                    var enum2 = enumerators[1];
                    bool available1 = enum1.MoveNext();
                    bool available2 = enum2.MoveNext();
                    IndexEntry current;
                    var restart = false;
                    do
                    {
                        restart = false;
                        while (available1 || available2)
                        {
                            var entry1 = new IndexEntry(enum1.Current.Stream, enum1.Current.Version, enum1.Current.Position);
                            var entry2 = new IndexEntry(enum2.Current.Stream, enum2.Current.Version, enum2.Current.Position);
                            if (version == PTableVersions.Index64Bit && enumerators[0].Table.Version == PTableVersions.Index32Bit)
                            {
                                var res = readRecord(entry1);
                                if (!res.Item2)
                                {
                                    available1 = enum1.MoveNext();
                                    restart = true;
                                    break;
                                }
                                entry1.Stream = upgradeHash(res.Item1, entry1.Stream);
                            }
                            if (version == PTableVersions.Index64Bit && enumerators[1].Table.Version == PTableVersions.Index32Bit)
                            {
                                var res = readRecord(entry2);
                                if (!res.Item2)
                                {
                                    available2 = enum2.MoveNext();
                                    restart = true;
                                    break;
                                }
                                entry2.Stream = upgradeHash(res.Item1, entry2.Stream);
                            }

                            if (available1 && (!available2 || entry1.CompareTo(entry2) > 0))
                            {
                                current = entry1;
                                available1 = enum1.MoveNext();
                            }
                            else
                            {
                                current = entry2;
                                available2 = enum2.MoveNext();
                            }

                            //Possibly doing another read if the record was read during the upgrade process
                            var item = readRecord(current);
                            if (item.Item2)
                            {
                                AppendRecordTo(bs, buffer, version, current, indexEntrySize);
                                dumpedEntryCount += 1;
                            }
                        }
                    } while (restart);
                    bs.Flush();
                    cs.FlushFinalBlock();

                    f.SetLength(f.Position + MD5Size);

                    // WRITE MD5
                    var hash = md5.Hash;
                    f.Write(hash, 0, hash.Length);
                    f.FlushToDisk();
                }
            }
            Log.Trace("PTables merge finished in {0} ([{1}] entries merged into {2}).",
                      watch.Elapsed, string.Join(", ", tables.Select(x => x.Count)), dumpedEntryCount);
            return new PTable(outputFile, Guid.NewGuid(), version, depth: cacheDepth);
        }

        private static long GetFileSize(IList<PTable> tables, int indexEntrySize)
        {
            long count = 0;
            for (int i = 0; i < tables.Count; ++i)
            {
                count += tables[i].Count;
            }
            return PTableHeader.Size + indexEntrySize * count + MD5Size;
        }

        private static int GetMaxOf(List<EnumerablePTable> enumerators, int version, Func<string, ulong, ulong> upgradeHash, Func<IndexEntry, Tuple<string, bool>> recordExistsAt)
        {
            //TODO GFY IF WE LIMIT THIS TO FOUR WE CAN UNROLL THIS LOOP AND WILL BE FASTER
            var max = new IndexEntry(ulong.MinValue, 0, long.MinValue);
            int idx = 0;
            var restart = false;
            do
            {
                restart = false;
                for (int i = 0; i < enumerators.Count; i++)
                {
                    var cur = enumerators[i].Current;
                    if (version == PTableVersions.Index64Bit && enumerators[i].Table.Version == PTableVersions.Index32Bit)
                    {
                        var res = recordExistsAt(cur);
                        if (!res.Item2)
                        {
                            if (!enumerators[i].MoveNext())
                            {
                                enumerators[i].Dispose();
                                enumerators.RemoveAt(i);
                            }
                            restart = true;
                            break;
                        }
                        if (res.Item2)
                        {
                            cur.Stream = upgradeHash(res.Item1, cur.Stream);
                        }
                    }
                    if (cur.CompareTo(max) > 0)
                    {
                        max = cur;
                        idx = i;
                    }
                }
            } while (restart);
            return idx;
        }

        private static void AppendRecordTo(Stream stream, byte[] buffer, int version, IndexEntry entry, int indexEntrySize)
        {
            if (version == PTableVersions.Index32Bit){
                var entry32 = new IndexEntry32((uint)entry.Stream, entry.Version, entry.Position);
                Marshal.Copy((IntPtr)entry32.Bytes, buffer, 0, indexEntrySize);
                stream.Write(buffer, 0, indexEntrySize);
            }
            else {
                Marshal.Copy((IntPtr)entry.Bytes, buffer, 0, indexEntrySize);
                stream.Write(buffer, 0, indexEntrySize);
            }
        }

        internal struct IndexEntryToMerge
        {
            public readonly IndexEntry Entry;
            public readonly bool Upgraded;
            public readonly bool Exists;
            public IndexEntryToMerge(IndexEntry indexEntry) : this(indexEntry, false, false) { }
            public IndexEntryToMerge(IndexEntry indexEntry, bool exists, bool upgraded)
            {
                Entry = indexEntry;
                Upgraded = upgraded;
                Exists = exists;
            }
        }

        internal class EnumerablePTable : IEnumerator<IndexEntry>
        {
            public PTable Table;
            IEnumerator<IndexEntry> Enumerator;

            public IndexEntry Current
            {
                get
                {
                    return Enumerator.Current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Enumerator.Current;
                }
            }

            public EnumerablePTable(PTable table, IEnumerator<IndexEntry> enumerator)
            {
                Table = table;
                Enumerator = enumerator;
            }

            public IEnumerator<IndexEntry> GetEnumerator()
            {
                return Enumerator;
            }

            public void Dispose()
            {
                Enumerator.Dispose();
            }

            public bool MoveNext()
            {
                return Enumerator.MoveNext();
            }

            public void Reset()
            {
                Enumerator.Reset();
            }
        }
    }
}
