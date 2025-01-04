using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.IO
{
    public class ByteArraysStream : Stream
    {
        private byte[][] _byteArrays;
        private int[] _sizeSums;
        private int _length;
        private int _position;
        private int _curIndex;

        public ByteArraysStream(byte[][] byteArrays)
        {
            _byteArrays = byteArrays;
            _sizeSums = new int[byteArrays.Length + 1];
            _position = 0;
            _curIndex = 0;
            for(int i = 1; i <= byteArrays.Length; i++)
            {
                _sizeSums[i] = _sizeSums[i - 1] + byteArrays[i - 1].Length;
            }
            _length = _sizeSums[byteArrays.Length];
        }
        
        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => _length;

        public override long Position
        {
            get => _position;
            set
            {
                Seek(value, SeekOrigin.Begin);
            }
        }

        public override void Flush()
        {
            
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int readCount = Math.Min(_length - _position, count);
            int output = readCount;
            int bufferIndex = offset;
            while(readCount > 0)
            {
                int curArrIndex = _position - _sizeSums[_curIndex];
                byte[] curArr = _byteArrays[_curIndex];
                int copyLen = Math.Min(readCount, curArr.Length - curArrIndex);
                Array.Copy(curArr, curArrIndex, buffer, bufferIndex, copyLen);
                if ((readCount >= (curArr.Length - curArrIndex)))
                    _curIndex++;
                _position += copyLen;
                readCount -= copyLen;
                bufferIndex += copyLen;
            }
            return readCount > 0 ? readCount : 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _position = (int)offset;
                    break;
                case SeekOrigin.Current:
                    _position = _position + (int)offset;
                    break;
                case SeekOrigin.End:
                    _position = _length - (int)offset;
                    break;
            }
            if (_position > _length || _position < 0)
                throw new Exception();
            _curIndex = findArraysIndex((int)_position);
            return _position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            
        }

        private int findArraysIndex(int position)
        {
            int begin = 0;
            int end = _sizeSums.Length;
            while((begin + 1) < end)
            {
                int mid = (begin + end) >> 1;
                if (position < _sizeSums[mid])
                    end = mid - 1;
                else if (position == _sizeSums[mid])
                    return mid;
                else
                    begin = mid;
            }
            return begin;
        }
    }
}
