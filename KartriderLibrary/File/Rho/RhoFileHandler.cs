using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.File
{
    public class RhoFileHandler
    {
        internal uint _fileDataIndex;
        internal uint _key;
        internal int _size;
        internal RhoFileProperty _fileProperty;

        private RhoArchive _archive;
        private bool _released;
        private bool _disposed;

        internal RhoFileHandler(RhoArchive archive, RhoFileProperty fileProperty, uint fileDataIndex, int size, uint key)
        {
            _fileDataIndex = fileDataIndex;
            _fileProperty = fileProperty;
            _archive = archive;
            _size = size;
            _key = key;

            _released = false;
            _disposed = false;
        }

        internal byte[] getData()
        {
            if (_released)
                throw new Exception("this handle was released.");
            return _archive.getData(this);
        }

        internal void releaseHandler()
        {
            _released = true;
        }
    }
}