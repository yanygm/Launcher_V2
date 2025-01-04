using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.IO
{
    public class NamedObject: KartObject
    {
        public string Name { get; set; } = "";
        protected NamedObject() 
        {
            
        }
        public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object> decodedFieldMap)
        {
            Name = reader.ReadKRString();
        }
        public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object> decodedFieldMap)
        {
            writer.WriteKRString(Name);
        }
        public override string ToString()
        {
            return $"{this.GetType().Name}: {Name}";
        }
    }
}
