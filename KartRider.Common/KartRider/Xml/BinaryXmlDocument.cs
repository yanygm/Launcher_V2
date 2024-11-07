using System;
using System.IO;
using System.Text;
using System.Xml;
using KartLibrary.IO;

namespace KartLibrary.Xml;

public class BinaryXmlDocument
{
    private BinaryXmlTag _rootTag;

    public BinaryXmlTag RootTag => _rootTag;

    public BinaryXmlDocument()
    {
        _rootTag = new BinaryXmlTag();
    }

    public void Read(Encoding encoding, byte[] array)
    {
        new BinaryXmlTag();
        using MemoryStream input = new MemoryStream(array);
        BinaryReader br = new BinaryReader(input);
        _rootTag = br.ReadBinaryXmlTag(encoding);
    }

    public void ReadFromXml(string XML)
    {
        XmlDocument xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(XML);
        if (xmlDocument.ChildNodes.Count < 1)
        {
            throw new Exception("there are no any nodes in this XML document.");
        }

        if (xmlDocument.ChildNodes.Count > 1)
        {
            throw new Exception("there are more than one root nodes in this XML document.");
        }

        _rootTag = (BinaryXmlTag)(xmlDocument.ChildNodes[0] ?? throw new Exception(""));
    }

    public void ReadFromXml(byte[] EncodedXML)
    {
        XmlDocument xmlDocument = new XmlDocument();
        using MemoryStream inStream = new MemoryStream(EncodedXML);
        xmlDocument.Load(inStream);
        if (xmlDocument.ChildNodes.Count < 1)
        {
            throw new Exception("there are no any nodes in this XML document.");
        }

        if (xmlDocument.ChildNodes.Count > 1)
        {
            throw new Exception("there are more than one root nodes in this XML document.");
        }

        _rootTag = (BinaryXmlTag)(xmlDocument.ChildNodes[0] ?? throw new Exception(""));
    }
}