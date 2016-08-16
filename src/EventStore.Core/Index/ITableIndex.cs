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
        void AddEntries(long commitPos, IList<IndexEntry32> entries);
        
        bool TryGetOneValue(string streamId, int version, out long position);
        bool TryGetLatestEntry(string streamId, out IndexEntry32 entry);
        bool TryGetOldestEntry(string streamId, out IndexEntry32 entry);

        IEnumerable<IndexEntry32> GetRange(string streamId, int startVersion, int endVersion, int? limit = null);
    }
}