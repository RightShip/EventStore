using System;
using System.Runtime.InteropServices;

namespace EventStore.Core.Index
{
    public struct IndexEntry: IComparable<IndexEntry>, IEquatable<IndexEntry>
    {
        public string StreamId;
        public UInt64 Key;
        public Int32 Version;
        public UInt64 Stream;
        public Int64 Position;

        public IndexEntry(string streamId, int version, long position)
        {
            StreamId = streamId;
            Key = 0;
            Version = version;
            Stream = 0;
            Position = position;
        }

        public IndexEntry(byte[] bytes)
        {
            StreamId = "";
            Key = BitConverter.ToUInt64(bytes, 0);
            Version = BitConverter.ToInt32(bytes, 0);
            Stream = BitConverter.ToUInt64(bytes, 4);
            Position = BitConverter.ToInt64(bytes, bytes.Length == 16 ? 8 : 12);
        }

        public IndexEntry(ulong key, long position) : this()
        {
            StreamId = "";
            Key = key;
            Stream = key >> 32;
            Version = (int)(Stream << 32 ^ Key);
            Position = position;
        }

        public IndexEntry(ulong stream, int version, long position) : this()
        {
            StreamId = "";
            Stream = stream;
            Version = version;
            Key = (ulong)version | stream << 32;
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