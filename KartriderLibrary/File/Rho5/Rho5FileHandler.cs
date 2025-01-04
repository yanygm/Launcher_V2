using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.File
{
    public class Rho5FileHandler
    {
        internal byte[] _key;
        internal byte[] _fileChksum;
        internal int _offset;
        internal int _decompressedSize;
        internal int _compressedSize;
        internal int _dataPackID;

        private Rho5Archive _archive;

        private bool _released;

        internal Rho5FileHandler(Rho5Archive archive, int dataPackID, int offset, int decompressedSize, int compressedSize, byte[] key, byte[] fileChksum)
        {
            _archive = archive;
            _dataPackID = dataPackID;
            _offset = offset;
            _decompressedSize = decompressedSize;
            _compressedSize = compressedSize;
            _key = key;
            _released = false;
            _fileChksum = fileChksum;
        }

        internal byte[] getData()
        {
            if (_released)
                throw new Exception("This handler was released.");
            return _archive.getData(this);
        }

        internal void releaseHandler()
        {
            _released = true;
        }
    }
}