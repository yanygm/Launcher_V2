using KartLibrary.Consts;
using KartLibrary.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Client
{
    internal class PinObject : KartObject
    {
        public override string ClassName => "PinObject";

        public short szId { get; set; }
        public CountryCode CountryCode { get; set; }
        public CountryCode AlternateCountryCode { get; set; }
        public short MajorId { get; set; }
        public short PackageVersion { get; set; }
        public short ClientVersion { get; set; }

    }
}