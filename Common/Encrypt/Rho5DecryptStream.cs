using System;
using System.IO;

namespace KartLibrary.Encrypt;

public class Rho5DecryptStream : Stream
{
    private byte[] Buffer = new byte[64];

    private int bufStartPos;

    private int bufferCount = 64;

    private int bufPos = 64;

    private bool Inited;

    public Stream BaseStream { get; set; }

    private Rho5KeyProvider KeyProvider { get; }

    public override bool CanRead => BaseStream.CanRead;

    public override bool CanSeek => BaseStream.CanSeek;

    public override bool CanWrite => false;

    public override long Length => BaseStream.Length;

    public override long Position
    {
        get
        {
            return bufStartPos + bufPos;
        }
        set
        {
            BaseStream.Position = value;
            bufPos = (bufStartPos = 64);
        }
    }

    public Rho5DecryptStream(Stream BaseStream, byte[] Key)
    {
        this.BaseStream = BaseStream;
        KeyProvider = new Rho5KeyProvider();
        KeyProvider.InitFromKey(Key);
        Inited = true;
        bufPos = (bufStartPos = 64);
    }

    public Rho5DecryptStream(Stream BaseStream, string fileName, string anotherData)
    {
        this.BaseStream = BaseStream;
        KeyProvider = new Rho5KeyProvider();
        KeyProvider.InitHeaderKey(fileName, anotherData);
        Inited = true;
        bufPos = (bufStartPos = 64);
    }

    public Rho5DecryptStream(Stream BaseStream)
    {
        this.BaseStream = BaseStream;
        KeyProvider = new Rho5KeyProvider();
        Inited = false;
    }

    public override void Flush()
    {
        BaseStream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int num = count;
        int num2 = offset;
        while (num > 0)
        {
            if (bufPos >= bufferCount && !refreshBuffer())
            {
                return count - num;
            }

            int num3 = Math.Min(bufferCount - bufPos, num);
            Array.Copy(Buffer, bufPos, buffer, num2, num3);
            bufPos += num3;
            num2 += num3;
            num -= num3;
        }

        return count;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        BaseStream.Seek(offset, origin);
        long position = BaseStream.Position;
        bufferCount = 64;
        bufPos = 64;
        return position;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    private unsafe bool refreshBuffer()
    {
        bufStartPos = (int)BaseStream.Position;
        int num = BaseStream.Read(Buffer, 0, 64);
        if (num <= 0)
        {
            return false;
        }

        int num2 = num + 3 >> 2;
        fixed (byte* ptr = Buffer)
        {
            uint* ptr2 = (uint*)ptr;
            for (int i = 0; i < num2; i++)
            {
                uint nextSubNum = KeyProvider.GetNextSubNum();
                ptr2[i] -= nextSubNum;
            }
        }

        bufPos = 0;
        bufferCount = num;
        return true;
    }

    public void SetToHeaderKey(string fileName, string anotherData)
    {
        KeyProvider.InitHeaderKey(fileName, anotherData);
    }

    public void SetToFilesInfoKey(string fileName, string anotherData)
    {
        KeyProvider.InitFilesInfoKey(fileName, anotherData);
    }

    ~Rho5DecryptStream()
    {
        Buffer = null;
    }
}