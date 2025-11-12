using Launcher.Library.Constant;
using Launcher.Library.IO;

namespace Launcher.Library.Client
{
    internal class PinObject : KartObject
    {
        public override string ClassName => "PinObject";
        public short SzId { get; set; }
        public CountryCode CountryCode { get; set; }
        public CountryCode AlternateCountryCode { get; set; }
        public short MajorId { get; set; }
        public short PackageVersion { get; set; }
        public short ClientVersion { get; set; }
    }
}
