using System;

namespace KartLibrary.File;

public interface IRhoArchive<TFolder, TFile> : IDisposable where TFolder : IRhoFolder<TFolder, TFile> where TFile : IRhoFile
{
    TFolder RootFolder { get; }
}