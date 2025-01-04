using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using KartLibrary.IO;
using System.Xml;

namespace KartLibrary.Xml
{
    public class BinaryXmlDocument
    {
        private BinaryXmlTag _rootTag;

        public BinaryXmlTag RootTag => _rootTag;

        public BinaryXmlDocument()
        {
            _rootTag = new BinaryXmlTag();
        }

        public void Read(Encoding encoding,byte[] array)
        {
            BinaryXmlTag tag = new BinaryXmlTag();
            using(MemoryStream ms = new MemoryStream(array))
            {
                BinaryReader br = new BinaryReader(ms);
                _rootTag = br.ReadBinaryXmlTag(encoding);
            }
        }

        public void ReadFromXml(string XML)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(XML);
            if (xmlDoc.ChildNodes.Count < 1)
                throw new Exception("there are no any nodes in this XML document.");
            if (xmlDoc.ChildNodes.Count > 1)
                throw new Exception("there are more than one root nodes in this XML document.");
            _rootTag = (BinaryXmlTag)(xmlDoc.ChildNodes[0] ?? throw new Exception(""));
        }

        public void ReadFromXml(byte[] EncodedXML)
        {
            XmlDocument xmlDoc = new XmlDocument();
            using(MemoryStream ms = new MemoryStream(EncodedXML))
            {
                xmlDoc.Load(ms);
                if (xmlDoc.ChildNodes.Count < 1)
                    throw new Exception("there are no any nodes in this XML document.");
                if (xmlDoc.ChildNodes.Count > 1)
                    throw new Exception("there are more than one root nodes in this XML document.");
                _rootTag = (BinaryXmlTag)(xmlDoc.ChildNodes[0] ?? throw new Exception(""));
            }
        }
    }
}
