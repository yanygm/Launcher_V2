using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.File
{
    /// <summary>
    /// The modes of storing a <see cref="RhoArchive"/> when using <see cref="KartStorageSystem"/>.
    /// </summary>
    public enum RhoFolderStoreMode
    {
        None,
        PackFolder,
        RhoFolder,
    }
}