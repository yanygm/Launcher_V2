using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.File
{
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

    public interface IRhoFolder<TFolder, TFile> : IRhoFolder where TFolder : IRhoFolder<TFolder, TFile> where TFile : IRhoFile
    {
        new IReadOnlyCollection<TFile> Files { get; }

        new IReadOnlyCollection<TFolder> Folders { get; }

        new TFile? GetFile(string path);

        new TFolder? GetFolder(string path);
    }

}