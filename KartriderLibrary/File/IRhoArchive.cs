using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.File
{
    public interface IRhoArchive<TFolder, TFile> : IDisposable where TFile : IRhoFile where TFolder : IRhoFolder<TFolder, TFile>
    {
        TFolder RootFolder { get; }
    }
}