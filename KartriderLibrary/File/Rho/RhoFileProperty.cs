using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.File
{
    public enum RhoFileProperty
    {
        None = 0x00,
        Compressed = 0x01,
        Encrypted = 0x04,
        PartialEncrypted = 0x05,
        CompressedEncrypted = 0x06
    }
}