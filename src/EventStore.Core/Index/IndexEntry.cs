using System;

namespace EventStore.Core.Index
{
    public struct IndexEntry: IComparable<IndexEntry>, IEquatable<IndexEntry>
    {
        public UInt64 Key;
        public string StreamId;
        public byte[] Bytes;
        public Int32 Version;
        public UInt64 Stream;
        public Int64 Position;
        public IndexEntry(ulong key, long position) : this()
        {
            Key = key;
            Position = position;
        }

        public IndexEntry(ulong stream, int version, long position) : this()
        {
            Stream = stream;
            Version = version;
            Position = position;
        }

        public IndexEntry(string streamid, int version, long position) : this()
        {
            StreamId = streamid;
            Version = version;
            Position = position;
        }

        public int CompareTo(IndexEntry other)
        {
            var keyCmp = Key.CompareTo(other.Key);
            if (keyCmp != 0)
                return keyCmp;
            return Position.CompareTo(other.Position);
        }

        public bool Equals(IndexEntry other)
        {
            return Key == other.Key && Position == other.Position;
        }

        public override string ToString()
        {
            return string.Format("Key: {0}, Stream: {1}, Version: {2}, Position: {3}", Key, Stream, Version, Position);
        }
    }
}