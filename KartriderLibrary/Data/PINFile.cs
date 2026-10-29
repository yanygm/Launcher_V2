using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using KartRider.Common.Security;
using KartRider.IO.Packet;

namespace KartRider.Common.Data
{
    public class PINFile
    {
        public class PINHeader
        {
            public byte Unk1 { get; set; }

            public ushort LocaleID { get; set; }

            public ushort Unk2 { get; set; }

            public byte LocaleType { get; set; }

            public byte Unk3 { get; set; }

            public ushort MinorVersion { get; set; }

            public byte Unk4 { get; set; }

            public byte Unk5 { get; set; }

            public byte LoginType { get; set; }

            public string AESKey { get; set; }

            public string URL { get; set; }

            public string PatchURL { get; set; }

            public PINHeader()
            {
                AESKey = (URL = (PatchURL = string.Empty));
            }
        }

        public class IPEndPoint
        {
            public string IP { get; set; }

            public ushort Port { get; set; }

            public void Read(InPacket p)
            {
                IP = $"{p.ReadByte()}.{p.ReadByte()}.{p.ReadByte()}.{p.ReadByte()}";
                Port = p.ReadUShort();
            }

            public void Write(OutPacket p)
            {
                IPAddress ip = IPAddress.Parse(IP);
                p.WriteEndPoint(ip, Port);
            }

            public override string ToString()
            {
                return IP + ":" + Port;
            }
        }

        public class AuthMethod
        {
            public byte Index { get; set; }

            public string Name { get; set; }

            public List<IPEndPoint> LoginServers { get; set; }

            public BmlObject AccountConfig { get; set; }

            public BmlObject ExtraConfig { get; set; }

            public AuthMethod()
            {
                Name = string.Empty;
            }
        }

        private const uint RTTI_PINOBJECT = 284099454u;

        public PINHeader Header { get; set; }

        public List<AuthMethod> AuthMethods { get; set; }

        public List<BmlObject> BmlObjects { get; set; }

        public int NullsBeforeBmlObjects { get; private set; }

        public PINFile()
        {
            Header = new PINHeader();
            AuthMethods = new List<AuthMethod>();
            BmlObjects = new List<BmlObject>();
        }

        public PINFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new Exception("Unable to locate PIN file.");
            }

            byte[] input = File.ReadAllBytes(path);
            Read(Decrypt(input));
        }

        public byte[] GetEncryptedData()
        {
            return Encrypt(this);
        }

        private static InPacket Decrypt(byte[] input)
        {
            InPacket inPacket = new InPacket(input);
            byte[] inputBytes = inPacket.ReadBytes(inPacket.ReadInt());
            return new InPacket(KREncodedBlock.Decode(inputBytes));
        }

        private static byte[] Encrypt(PINFile pinFile)
        {
            OutPacket outPacket = new OutPacket();
            pinFile.Write(outPacket);
            OutPacket outPacket2 = new OutPacket();
            byte[] array = KREncodedBlock.Encode(outPacket.ToArray(), (KREncodedBlock.EncodeFlag)3, 862557747u);
            outPacket2.WriteInt(array.Length);
            outPacket2.WriteBytes(array);
            return outPacket2.ToArray();
        }

        private void Read(InPacket p)
        {
            if (p.ReadUInt() != 284099454)
            {
                throw new InvalidDataException("This is not a valid PIN file.");
            }

            Header = new PINHeader();
            Header.Unk1 = p.ReadByte();
            Header.LocaleID = p.ReadUShort();
            Header.Unk2 = p.ReadUShort();
            Header.LocaleType = p.ReadByte();
            Header.Unk3 = p.ReadByte();
            Header.MinorVersion = p.ReadUShort();
            Header.Unk4 = p.ReadByte();
            Header.Unk5 = p.ReadByte();
            Header.LoginType = p.ReadByte();
            Header.AESKey = p.ReadString();
            Header.URL = p.ReadString();
            Header.PatchURL = p.ReadString();
            AuthMethods = new List<AuthMethod>();
            for (int num = p.ReadInt(); num > 0; num--)
            {
                AuthMethod authMethod = new AuthMethod
                {
                    Index = p.ReadByte(),
                    Name = p.ReadString(),
                    AccountConfig = ReadBML(p),
                    LoginServers = new List<IPEndPoint>()
                };
                for (int num2 = p.ReadInt(); num2 > 0; num2--)
                {
                    IPEndPoint iPEndPoint = new IPEndPoint();
                    iPEndPoint.Read(p);
                    authMethod.LoginServers.Add(iPEndPoint);
                }

                authMethod.ExtraConfig = ReadBML(p);
                AuthMethods.Add(authMethod);
            }

            while (p.Available > 0)
            {
                int position = p.Position;
                if (p.ReadByte() == 0)
                {
                    NullsBeforeBmlObjects++;
                    continue;
                }

                p.Position = position;
                break;
            }

            BmlObjects = new List<BmlObject>();
            while (p.Available > 0)
            {
                BmlObject bmlObject = ReadBML(p);
                if (bmlObject != null)
                {
                    BmlObjects.Add(bmlObject);
                }
            }
        }

        private void Write(OutPacket p)
        {
            p.WriteUInt(284099454u);
            p.WriteByte(Header.Unk1);
            p.WriteUShort(Header.LocaleID);
            p.WriteUShort(Header.Unk2);
            p.WriteByte(Header.LocaleType);
            p.WriteByte(Header.Unk3);
            p.WriteUShort(Header.MinorVersion);
            p.WriteByte(Header.Unk4);
            p.WriteByte(Header.Unk5);
            p.WriteByte(Header.LoginType);
            p.WriteString(Header.AESKey);
            p.WriteString(Header.URL);
            p.WriteString(Header.PatchURL);
            p.WriteInt(AuthMethods.Count);
            foreach (AuthMethod authMethod in AuthMethods)
            {
                p.WriteByte(authMethod.Index);
                p.WriteString(authMethod.Name);
                WriteBML(p, authMethod.AccountConfig);
                p.WriteInt(authMethod.LoginServers.Count);
                foreach (IPEndPoint loginServer in authMethod.LoginServers)
                {
                    loginServer.Write(p);
                }

                WriteBML(p, authMethod.ExtraConfig);
            }

            p.WriteBytes(new byte[NullsBeforeBmlObjects]);
            foreach (BmlObject bmlObject in BmlObjects)
            {
                WriteBML(p, bmlObject);
            }
        }

        private BmlObject ReadBML(InPacket p)
        {
            return (!p.ReadBool()) ? null : new BmlObject(p);
        }

        private void WriteBML(OutPacket p, BmlObject bml)
        {
            p.WriteBool(bml != null);
            bml?.Save(p);
        }
    }
}