using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KartLibrary.Encrypt;
using KartLibrary.File;

namespace KartLibrary.File
{
    [Obsolete("RhoFileInfo class is deprecated. Use RhoFile instead.")]
    public class RhoFileInfo
    {
        public Rho BaseRho { get; set; }
        public string Name { get; set; }
        private string _ext = "";
        private int extnum = -1;
        public string Extension
        {
            get
            {
                return _ext;
            }
            set
            {
                _ext = value;
                extnum = getExtNum();
            }
        }
        public int ExtNum => extnum;
        public uint FileBlockIndex { get; set; }
        public RhoFileProperty FileProperty { get; set; }
        public int FileSize { get; set; }
        internal RhoFileInfo(Rho baseRho)
        {
            BaseRho = baseRho;
        }
        public byte[] GetData()
        {
            uint DecryptKey = RhoKey.GetDataKey(BaseRho.GetFileKey(), this);
            byte[] Data = BaseRho.GetBlockData(FileBlockIndex, DecryptKey);
            return Data;
        }
        internal int getExtNum()
        {
            int output = 0;
            byte[] arr = Encoding.UTF8.GetBytes(_ext);
            for (int i = 0; i < arr.Length; i++)
            {
                output |= arr[i] << (i << 3);
            }
            return output;
        }

        public string FullFileName
        {
            get
            {
                if (_ext == "")
                    return Name;
                else
                    return $"{Name}.{_ext}";
            }
        }

        public RhoFileStream GetStream()
        {
            return new RhoFileStream(this);
        }
    }


}