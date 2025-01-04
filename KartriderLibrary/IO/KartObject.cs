using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.IO
{
    public abstract class KartObject
    {
        protected KartObject() 
        {
            //if (!KartObjectManager.ContainsClass(ClassStamp))
            //    KartObjectManager.RegisterClass(this.GetType());
        }
        public virtual string ClassName => this.GetType().Name;

        public uint ClassStamp
        {
            get
            {
                byte[] classNameEnc = Encoding.UTF8.GetBytes(ClassName);
                return Adler.Adler32(0, classNameEnc, 0, classNameEnc.Length);
            }
        }

        public virtual void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap) 
        {
            
        }
        public virtual void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {

        }
    }
}
