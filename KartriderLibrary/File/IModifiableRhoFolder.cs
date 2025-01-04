using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.File
{

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

        // Methods for modification

        void AddFile(IModifiableRhoFile file);

        void AddFile(string path, IModifiableRhoFile file);

        void AddFolder(IModifiableRhoFolder folder);

        void AddFolder(string path, IModifiableRhoFolder folder);

        bool RemoveFile(string fileFullName);

        bool RemoveFolder(string folderFullName);
    }

    /// <summary>
    /// A generic <see cref="IModifiableRhoFolder"/>. See also <seealso cref="IModifiableRhoFolder"/>
    /// </summary>
    /// <typeparam name="TFolder">Type of folder can be stored in a instance of this type.</typeparam>
    /// <typeparam name="TFile"></typeparam>
    public interface IModifiableRhoFolder<TFolder, TFile> : IModifiableRhoFolder where TFolder : IModifiableRhoFolder where TFile : IModifiableRhoFile
    {
        new IReadOnlyCollection<TFile> Files { get; }

        new IReadOnlyCollection<TFolder> Folders { get; }

        new TFile? GetFile(string path);

        new TFolder? GetFolder(string path);

        // Methods for modification

        void AddFile(TFile file);

        void AddFile(string path, TFile file);

        void AddFolder(TFolder folder);

        void AddFolder(string path, TFolder folder);

        bool RemoveFile(TFile fileToRemove);

        bool RemoveFolder(TFolder folderToRemove);
    }
}