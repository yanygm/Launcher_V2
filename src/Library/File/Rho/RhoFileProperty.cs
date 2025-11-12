namespace Launcher.Library.File.Rho
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