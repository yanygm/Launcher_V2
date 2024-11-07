using System;
using System.Collections.Generic;

namespace KartLibrary.File;

public interface IModifiableRhoFolder : IDisposable
{
    IModifiableRhoFolder? Parent { get; }

    IReadOnlyCollection<IModifiableRhoFile> Files { get; }

    IReadOnlyCollection<IModifiableRhoFolder> Folders { get; }

    string Name { get; set; }

    string FullName { get; }

    IModifiableRhoFile? GetFile(string path);

    IModifiableRhoFolder? GetFolder(string path);

    bool ContainsFile(string path);

    bool ContainsFolder(string path);

    void AddFile(IModifiableRhoFile file);

    void AddFile(string path, IModifiableRhoFile file);

    void AddFolder(IModifiableRhoFolder folder);

    void AddFolder(string path, IModifiableRhoFolder folder);

    bool RemoveFile(string fileFullName);

    bool RemoveFolder(string folderFullName);
}