namespace Launcher.Library.File
{
    public interface IRhoArchive<TFolder, TFile> : IDisposable where TFile : IRhoFile where TFolder : IRhoFolder<TFolder, TFile>
    {
        TFolder RootFolder { get; }
    }
}