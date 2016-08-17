using System.Collections.Generic;

namespace EventStore.Core.Index
{
    public interface ITableIndex
    {
        long CommitCheckpoint { get; }
        long PrepareCheckpoint { get; }

        void Initialize(long chaserCheckpoint);
        void Close(bool removeFiles = true);

        void Add(long commitPos, ulong stream, int version, long position);
        void AddEntries(long commitPos, IList<IndexEntry> entries);
        
        bool TryGetOneValue(ulong stream, int version, out long position);
        bool TryGetLatestEntry(ulong stream, out IndexEntry entry);
        bool TryGetOldestEntry(ulong stream, out IndexEntry entry);

        IEnumerable<IndexEntry> GetRange(ulong stream, int startVersion, int endVersion, int? limit = null);
    }
}