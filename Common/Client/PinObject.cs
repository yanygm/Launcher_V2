using KartLibrary.Consts;
using KartLibrary.IO;

#nullable enable
namespace KartLibrary.Client
{
    internal class PinObject : KartObject
    {
        public override string ClassName => nameof(PinObject);

        public short szId { get; set; }

        public CountryCode CountryCode { get; set; }

        public CountryCode AlternateCountryCode { get; set; }

        public short MajorId { get; set; }

        public short PackageVersion { get; set; }

        public short ClientVersion { get; set; }
    }
}
