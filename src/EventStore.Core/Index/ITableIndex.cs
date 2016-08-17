using System.Collections.Generic;

namespace EventStore.Core.Index
{
    public interface ITableIndex
    {
        long CommitCheckpoint { get; }
        long PrepareCheckpoint { get; }

        void Initialize(long chaserCheckpoint);
        void Close(bool removeFiles = true);

        void Add(long commitPos, string streamId, int version, long position);
        void AddEntries(long commitPos, IList<IndexEntryFoo> entries);
        
        bool TryGetOneValue(string streamId, int version, out long position);
        bool TryGetLatestEntry(string streamId, out IndexEntry entry);
        bool TryGetOldestEntry(string streamId, out IndexEntry entry);

        IEnumerable<IndexEntry> GetRange(string streamId, int startVersion, int endVersion, int? limit = null);
    }
}