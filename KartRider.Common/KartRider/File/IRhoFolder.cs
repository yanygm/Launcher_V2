using System;
using System.Collections.Generic;

namespace KartLibrary.File;

public interface IRhoFolder : IDisposable
{
    IRhoFolder? Parent { get; }

    IReadOnlyCollection<IRhoFile> Files { get; }

    IReadOnlyCollection<IRhoFolder> Folders { get; }

    string Name { get; }

    string FullName { get; }

    IRhoFile? GetFile(string path);

    IRhoFolder? GetFolder(string path);

    bool ContainsFile(string path);

    bool ContainsFolder(string path);
}