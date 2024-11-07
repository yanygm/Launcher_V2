using System;
using System.IO;

namespace KartLibrary.IO;

public class ByteArraysStream : Stream
{
    private byte[][] _byteArrays;

    private int[] _sizeSums;

    private int _length;

    private int _position;

    private int _curIndex;

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => true;

    public override long Length => _length;

    public override long Position
    {
        get
        {
            return _position;
        }
        set
        {
            Seek(value, SeekOrigin.Begin);
        }
    }

    public ByteArraysStream(byte[][] byteArrays)
    {
        _byteArrays = byteArrays;
        _sizeSums = new int[byteArrays.Length + 1];
        _position = 0;
        _curIndex = 0;
        for (int i = 1; i <= byteArrays.Length; i++)
        {
            _sizeSums[i] = _sizeSums[i - 1] + byteArrays[i - 1].Length;
        }

        _length = _sizeSums[byteArrays.Length];
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int num = Math.Min(_length - _position, count);
        int num2 = offset;
        while (num > 0)
        {
            int num3 = _position - _sizeSums[_curIndex];
            byte[] array = _byteArrays[_curIndex];
            int num4 = Math.Min(num, array.Length - num3);
            Array.Copy(array, num3, buffer, num2, num4);
            if (num >= array.Length - num3)
            {
                _curIndex++;
            }

            _position += num4;
            num -= num4;
            num2 += num4;
        }

        if (num <= 0)
        {
            return 0;
        }

        return num;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                _position = (int)offset;
                break;
            case SeekOrigin.Current:
                _position += (int)offset;
                break;
            case SeekOrigin.End:
                _position = _length - (int)offset;
                break;
        }

        if (_position > _length || _position < 0)
        {
            throw new Exception();
        }

        _curIndex = findArraysIndex(_position);
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
        int num = 0;
        int num2 = _sizeSums.Length;
        while (num + 1 < num2)
        {
            int num3 = num + num2 >> 1;
            if (position < _sizeSums[num3])
            {
                num2 = num3 - 1;
                continue;
            }

            if (position == _sizeSums[num3])
            {
                return num3;
            }

            num = num3;
        }

        return num;
    }
}