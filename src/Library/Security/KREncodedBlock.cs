using Ionic.Zlib;
using Launcher.Library.IO;
using Launcher.Library.Utilities;
using System.Net;

namespace Launcher.Library.Security
{
    public class KREncodedBlock
    {
        public KREncodedBlock()
        {
        }

        public static byte[] Decode(byte[] inputBytes)
        {
            byte[] numArray;
            uint num;
            uint num1;
            InPacket inPacket = new InPacket(inputBytes);
            if (inPacket.ReadByte() == 83)
            {
                EncodeFlag encodeFlag = (EncodeFlag)inPacket.ReadByte();
                uint num2 = inPacket.ReadUInt();
                if ((int)(encodeFlag & EncodeFlag.KartCrypto) != 0)
                {
                    num = inPacket.ReadUInt();
                }
                else
                {
                    num = 0;
                }
                uint num3 = num;
                if ((int)(encodeFlag & EncodeFlag.ZLib) != 0)
                {
                    num1 = inPacket.ReadUInt();
                }
                else
                {
                    num1 = 0;
                }
                uint num4 = num1;
                byte[] array = inPacket.ReadBytes(inPacket.Available);
                if ((int)(encodeFlag & EncodeFlag.KartCrypto) != 0)
                {
                    array = KRCrypto.ApplyCrypto(array, num3);
                }
                if ((int)(encodeFlag & EncodeFlag.ZLib) != 0)
                {
                    if (array[0] == 120 ? false : array[1] != 218)
                    {
                        throw new Exception("Invalid magic header! (zlib)");
                    }
                    int num5 = BitConverter.ToInt32(array, array.Length - 4);
                    byte[] numArray1 = new byte[array.Length - 2];
                    Buffer.BlockCopy(array, 2, numArray1, 0, numArray1.Length);
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (MemoryStream memoryStream1 = new MemoryStream(numArray1))
                        {
                            using (DeflateStream deflateStream = new DeflateStream(memoryStream1, CompressionMode.Decompress))
                            {
                                deflateStream.CopyTo(memoryStream);
                                deflateStream.Close();
                                array = memoryStream.ToArray();
                            }
                        }
                    }
                    if (num4 != (ulong)array.Length)
                    {
                        throw new Exception("Length was not equal");
                    }
                    if (Adler32Helper.GenerateSimpleAdler32(array) != IPAddress.HostToNetworkOrder(num5))
                    {
                        throw new Exception("Invalid checksum!");
                    }
                }
                if (Adler32Helper.GenerateAdler32(array, 0) != num2)
                {
                    throw new Exception("Checksums didnt match.");
                }
                if ((int)(encodeFlag & EncodeFlag.ZLib) == 0 ? false : num4 != (ulong)array.Length)
                {
                    throw new Exception("Lengths did not match");
                }
                numArray = array;
            }
            else
            {
                numArray = inputBytes;
            }
            return numArray;
        }

        public static byte[] Encode(byte[] inputBytes, EncodeFlag flag, uint? kartCryptoKey)
        {
            byte[] array;
            using (OutPacket outPacket = new OutPacket(64))
            {
                outPacket.WriteByte(83);
                outPacket.WriteByte((byte)flag);
                outPacket.WriteUInt(Adler32Helper.GenerateAdler32(inputBytes, 0));
                if ((int)(flag & EncodeFlag.KartCrypto) != 0)
                {
                    if (!kartCryptoKey.HasValue)
                    {
                        kartCryptoKey = new uint?(Adler32Helper.GenerateAdler32(inputBytes, 0));
                    }
                    outPacket.WriteUInt(kartCryptoKey.Value);
                }
                if ((int)(flag & EncodeFlag.ZLib) != 0)
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
                        {
                            using (MemoryStream memoryStream1 = new MemoryStream())
                            {
                                using (DeflateStream deflateStream = new DeflateStream(memoryStream1, CompressionMode.Compress))
                                {
                                    using (MemoryStream memoryStream2 = new MemoryStream(inputBytes))
                                    {
                                        memoryStream2.CopyTo(deflateStream);
                                    }
                                    deflateStream.Close();
                                    outPacket.WriteInt(inputBytes.Length);
                                    byte[] numArray = memoryStream1.ToArray();
                                    binaryWriter.Write(new byte[] { 120, 218 });
                                    binaryWriter.Write(numArray);
                                    binaryWriter.Write(IPAddress.HostToNetworkOrder(Adler32Helper.GenerateSimpleAdler32(inputBytes)));
                                    inputBytes = memoryStream.ToArray();
                                }
                            }
                        }
                    }
                }
                if ((int)(flag & EncodeFlag.KartCrypto) != 0)
                {
                    inputBytes = KRCrypto.ApplyCrypto(inputBytes, kartCryptoKey.Value);
                }
                outPacket.WriteBytes(inputBytes);
                array = outPacket.ToArray();
            }
            return array;
        }

        public enum EncodeFlag : byte
        {
            ZLib = 1,
            KartCrypto = 2
        }
    }
}