using System;

namespace KartLibrary.IO;

public class BytesReader
{
    private byte[] _baseData;

    private int _pos = -1;

    public BytesReader(byte[] baseData)
    {
        _baseData = baseData;
        _pos = 0;
    }

    public byte ReadByte()
    {
        if (_pos + 1 > _baseData.Length)
        {
            throw new IndexOutOfRangeException();
        }

        return _baseData[_pos++];
    }

    public sbyte ReadSByte()
    {
        if (_pos + 1 >= _baseData.Length)
        {
            throw new IndexOutOfRangeException();
        }

        return (sbyte)_baseData[_pos++];
    }

    public unsafe short ReadInt16()
    {
        if (_pos + 2 >= _baseData.Length)
        {
            throw new IndexOutOfRangeException();
        }

        fixed (byte* ptr = &_baseData[_pos])
        {
            _pos += 2;
            return *(short*)ptr;
        }
    }

    public unsafe ushort ReadUInt16()
    {
        if (_pos + 2 >= _baseData.Length)
        {
            throw new IndexOutOfRangeException();
        }

        fixed (byte* ptr = &_baseData[_pos])
        {
            _pos += 2;
            return *(ushort*)ptr;
        }
    }

    public unsafe int ReadInt32()
    {
        if (_pos + 4 >= _baseData.Length)
        {
            throw new IndexOutOfRangeException();
        }

        fixed (byte* ptr = &_baseData[_pos])
        {
            _pos += 4;
            return *(int*)ptr;
        }
    }

    public unsafe uint ReadUInt32()
    {
        if (_pos + 4 >= _baseData.Length)
        {
            throw new IndexOutOfRangeException();
        }

        fixed (byte* ptr = &_baseData[_pos])
        {
            _pos += 4;
            return *(uint*)ptr;
        }
    }

    public unsafe long ReadInt64()
    {
        if (_pos + 8 >= _baseData.Length)
        {
            throw new IndexOutOfRangeException();
        }

        fixed (byte* ptr = &_baseData[_pos])
        {
            _pos += 8;
            return *(long*)ptr;
        }
    }

    public unsafe ulong ReadUInt64()
    {
        if (_pos + 8 >= _baseData.Length)
        {
            throw new IndexOutOfRangeException();
        }

        fixed (byte* ptr = &_baseData[_pos])
        {
            _pos += 8;
            return *(ulong*)ptr;
        }
    }
}