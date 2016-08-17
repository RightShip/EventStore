using System;
namespace EventStore.Core.Index.Hashes
{
    public class CombinedHasher : IHasher
    {
    	private IHasher _lowHasher;
    	private IHasher _highHasher;
    	private bool _is64Bit;
    	public CombinedHasher(IHasher lowHasher, IHasher highHasher, bool is64Bit)
    	{
    		_lowHasher = lowHasher;
    		_highHasher = highHasher;
    		_is64Bit = is64Bit;
    	}
        public ulong CombinedHash(string streamId)
        {
            ulong hash = _lowHasher.Hash(streamId);
            if(_is64Bit){
                hash = hash << 32 | _highHasher.Hash(streamId);
            }
            return hash;
        }
        public uint Hash(string s){
        	throw new NotImplementedException();
        }
        public uint Hash(byte[] data){
        	throw new NotImplementedException();
        }
        public uint Hash(byte[] data, int offset, uint len, uint seed){
        	throw new NotImplementedException();
        }
    }
}