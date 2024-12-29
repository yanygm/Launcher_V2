using System;
using System.Collections.Generic;

namespace KartLibrary.File;

public interface IModifiableRhoFolder<TFolder, TFile> : IModifiableRhoFolder, IDisposable where TFolder : IModifiableRhoFolder where TFile : IModifiableRhoFile
{
    new IReadOnlyCollection<TFile> Files { get; }

    new IReadOnlyCollection<TFolder> Folders { get; }

    new TFile? GetFile(string path);

    new TFolder? GetFolder(string path);

    void AddFile(TFile file);

    void AddFile(string path, TFile file);

    void AddFolder(TFolder folder);

    void AddFolder(string path, TFolder folder);

    bool RemoveFile(TFile fileToRemove);

    bool RemoveFolder(TFolder folderToRemove);
}