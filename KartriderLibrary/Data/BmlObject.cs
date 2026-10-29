using System;
using System.Collections.Generic;
using System.IO;
using KartRider.IO.Packet;

namespace KartRider.Common.Data
{
    public class BmlObject
    {
        public string Name { get; set; }

        public string Value { get; set; }

        public Dictionary<string, string> Values { get; private set; }

        public List<Tuple<string, BmlObject>> SubObjects { get; private set; }

        public BmlObject()
        {
            Name = string.Empty;
            Value = string.Empty;
            Values = new Dictionary<string, string>();
            SubObjects = new List<Tuple<string, BmlObject>>();
        }

        public BmlObject(BmlObject copyObject)
            : this()
        {
            Name = copyObject.Name;
            Value = copyObject.Value;
            Values = new Dictionary<string, string>(copyObject.Values);
            foreach (Tuple<string, BmlObject> subObject in copyObject.SubObjects)
            {
                SubObjects.Add(new Tuple<string, BmlObject>(subObject.Item1, new BmlObject(subObject.Item2)));
            }
        }

        public BmlObject Copy()
        {
            return new BmlObject(this);
        }

        public BmlObject(InPacket p)
            : this()
        {
            Name = p.ReadString();
            Value = p.ReadString();
            int num = p.ReadInt();
            for (int i = 0; i < num; i++)
            {
                string key = p.ReadString();
                string value = p.ReadString();
                Values.Add(key, value);
            }

            int num2 = p.ReadInt();
            for (int j = 0; j < num2; j++)
            {
                BmlObject bmlObject = new BmlObject(p);
                SubObjects.Add(new Tuple<string, BmlObject>(bmlObject.Name, bmlObject));
            }
        }

        public static BmlObject Create(string path)
        {
            if (!File.Exists(path))
            {
                throw new Exception("Unable to locate object file.");
            }

            byte[] packet = File.ReadAllBytes(path);
            InPacket p = new InPacket(packet);
            return new BmlObject(p);
        }

        public void Save(OutPacket p)
        {
            p.WriteString(Name);
            p.WriteString(Value);
            p.WriteInt(Values.Count);
            foreach (KeyValuePair<string, string> value in Values)
            {
                p.WriteString(value.Key);
                p.WriteString(value.Value);
            }

            p.WriteInt(SubObjects.Count);
            foreach (Tuple<string, BmlObject> subObject in SubObjects)
            {
                subObject.Item2.Save(p);
            }
        }

        public BmlObject GetObject(string name)
        {
            foreach (Tuple<string, BmlObject> subObject in SubObjects)
            {
                if (subObject.Item1 == name)
                {
                    return subObject.Item2;
                }
            }

            return null;
        }

        public string GetString(string key, string def = "")
        {
            if (Values.ContainsKey(key))
            {
                return Values[key];
            }

            return def;
        }

        public bool GetBool(string key, bool def = false)
        {
            string text = GetString(key, def.ToString()).ToLower();
            if (text == "1" || text == "true")
            {
                return true;
            }

            if (text == "0" || text == "false")
            {
                return false;
            }

            return def;
        }

        public byte GetByte(string key, byte def = 0)
        {
            string @string = GetString(key, def.ToString());
            return byte.Parse(@string);
        }

        public short GetShort(string key, int def = 0)
        {
            string @string = GetString(key, def.ToString());
            return short.Parse(@string);
        }

        public ushort GetUShort(string key, int def = 0)
        {
            string @string = GetString(key, def.ToString());
            return ushort.Parse(@string);
        }

        public int GetInt(string key, int def = 0)
        {
            string @string = GetString(key, def.ToString());
            return int.Parse(@string);
        }

        public uint GetUInt(string key, int def = 0)
        {
            string @string = GetString(key, def.ToString());
            return uint.Parse(@string);
        }

        public float GetFloat(string key, float def = 0f)
        {
            string @string = GetString(key, def.ToString());
            return float.Parse(@string);
        }

        public void SetKeyValuePair(string key, string value)
        {
            if (Values.ContainsKey(key))
            {
                Values[key] = value;
            }
            else
            {
                Values.Add(key, value);
            }
        }

        public void SetBool(string key, bool value)
        {
            SetKeyValuePair(key, value ? "1" : "0");
        }
    }
}