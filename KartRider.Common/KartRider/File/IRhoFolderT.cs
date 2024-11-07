using System;
using System.Collections.Generic;

namespace KartLibrary.File;

public interface IRhoFolder<TFolder, TFile> : IRhoFolder, IDisposable where TFolder : IRhoFolder<TFolder, TFile> where TFile : IRhoFile
{
    new IReadOnlyCollection<TFile> Files { get; }

    new IReadOnlyCollection<TFolder> Folders { get; }

    new TFile? GetFile(string path);

    new TFolder? GetFolder(string path);
}