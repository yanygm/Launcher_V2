using Launcher.Library.Utilities;
using System.Net;
using System.Text;

namespace Launcher.Library.IO
{
    public class InPacket : PacketBase
    {
        private byte[] _buffer;

        private int _index;

        public int Available
        {
            get
            {
                return _buffer.Length - _index;
            }
        }

        public override int Length
        {
            get
            {
                return _buffer.Length;
            }
        }

        public override int Position
        {
            get
            {
                return _index;
            }
            set
            {
                _index = value;
            }
        }

        public InPacket(byte[] packet)
        {
            _buffer = packet;
            _index = 0;
        }

        private void CheckLength(int length)
        {
            if (_index + length > _buffer.Length ? true : length < 0)
            {
                throw new PacketReadException("Not enough space");
            }
        }

        public override void Dispose()
        {
            _buffer = null;
        }

        public bool ReadBool()
        {
            return ReadByte() == 1;
        }

        public byte ReadByte()
        {
            CheckLength(1);
            byte[] numArray = _buffer;
            int num = _index;
            _index = num + 1;
            return numArray[num];
        }

        public byte[] ReadBytes(int count)
        {
            CheckLength(count);
            byte[] numArray = new byte[count];
            Buffer.BlockCopy(_buffer, _index, numArray, 0, count);
            _index += count;
            return numArray;
        }


        public byte ReadEncodedByte()
        {
            return CryptoConstants.decodeBytes(new byte[] { ReadByte() })[0];
        }

        public float ReadEncodedFloat()
        {
            return BitConverter.ToSingle(CryptoConstants.decodeBytes(new byte[] { ReadByte(), ReadByte(), ReadByte(), ReadByte() }), 0);
        }

        public int ReadEncodedInt()
        {
            return BitConverter.ToInt32(CryptoConstants.decodeBytes(new byte[] { ReadByte(), ReadByte(), ReadByte(), ReadByte() }), 0);
        }

        public uint ReadEncodedUInt()
        {
            return BitConverter.ToUInt32(CryptoConstants.decodeBytes(new byte[] { ReadByte(), ReadByte(), ReadByte(), ReadByte() }), 0);
        }

        public IPEndPoint ReadEndPoint()
        {
            IPEndPoint pEndPoint = new IPEndPoint(new IPAddress(ReadBytes(4)), ReadUShort());
            return pEndPoint;
        }

        public uint ReadUInt()
        {
            return (uint)ReadInt();
        }

        public ulong ReadULong()
        {
            return (ulong)ReadLong();
        }

        public ushort ReadUShort()
        {
            return (ushort)ReadShort();
        }

        public sbyte ReadSByte()
        {
            return (sbyte)ReadByte();
        }

        public float ReadFloat()
        {
            return BitConverter.ToSingle(new byte[] { ReadByte(), ReadByte(), ReadByte(), ReadByte() }, 0);
        }

        public unsafe int ReadInt()
        {
            CheckLength(4);

            int value;

            fixed (byte* ptr = _buffer)
            {
                value = *(int*)(ptr + _index);
            }

            _index += 4;

            return value;
        }

        public unsafe long ReadLong()
        {
            CheckLength(8);

            long value;

            fixed (byte* ptr = _buffer)
            {
                value = *(long*)(ptr + _index);
            }

            _index += 8;

            return value;
        }

        public unsafe short ReadShort()
        {
            CheckLength(2);

            short value;

            fixed (byte* ptr = _buffer)
            {
                value = *(short*)(ptr + _index);
            }

            _index += 2;

            return value;
        }

        public string ReadString(bool ascii = false)
        {
            string str;
            int num = ReadInt();
            if (!ascii)
            {
                num *= 2;
            }
            CheckLength(num);
            str = !ascii ? Encoding.Unicode.GetString(ReadBytes(num)) : Encoding.ASCII.GetString(ReadBytes(num));
            return str;
        }

        public DateTime ReadTime()
        {
            DateTime dateTime;
            ushort num = ReadUShort();
            ushort num1 = ReadUShort();
            if (num != 65535)
            {
                uint num2 = (uint)((num * 21600) + num1);
                int num3 = (int)(num2 / 21600);
                int year = TimeUtil.GetYear(ref num3) + 1900;
                int month = TimeUtil.GetMonth(ref num3, TimeUtil.IsLeapYear(year)) + 1;
                int num4 = (int)(num2 % 21600 / 900);
                int num5 = (int)(num2 % 21600 % 900 / 15);
                int num6 = (int)(4 * (num2 % 21600 % 900 % 15));
                dateTime = new DateTime(year, month, num3, num4, num5, num6);
            }
            else
            {
                dateTime = DateTime.MinValue;
            }
            return dateTime;
        }

        public void Skip(int count)
        {
            CheckLength(count);
            _index += count;
        }

        public override byte[] ToArray()
        {
            byte[] numArray = new byte[_buffer.Length];
            Buffer.BlockCopy(_buffer, 0, numArray, 0, _buffer.Length);
            return numArray;
        }
    }
}