using System;
using System.Text;
using KartLibrary.Encrypt;

namespace KartLibrary.File;

public class RhoFileInfo
{
    private string _ext = "";

    private int extnum = -1;

    public Rho BaseRho { get; set; }

    public string Name { get; set; }

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

    public string FullFileName
    {
        get
        {
            if (_ext == "")
            {
                return Name;
            }

            return Name + "." + _ext;
        }
    }

    internal RhoFileInfo(Rho baseRho)
    {
        BaseRho = baseRho;
    }

    public byte[] GetData()
    {
        uint dataKey = RhoKey.GetDataKey(BaseRho.GetFileKey(), this);
        return BaseRho.GetBlockData(FileBlockIndex, dataKey);
    }

    internal int getExtNum()
    {
        int num = 0;
        byte[] bytes = Encoding.UTF8.GetBytes(_ext);
        for (int i = 0; i < bytes.Length; i++)
        {
            num |= bytes[i] << (i << 3);
        }

        return num;
    }

    public RhoFileStream GetStream()
    {
        return new RhoFileStream(this);
    }
}