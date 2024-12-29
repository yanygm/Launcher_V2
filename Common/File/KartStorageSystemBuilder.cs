using KartLibrary.Consts;

namespace KartLibrary.File;

public class KartStorageSystemBuilder
{
    private bool _useRho;

    private bool _useRho5;

    private bool _usePackFolderListFile;

    private string? _kartriderClientPath;

    private string? _kartriderDataPath;

    private CountryCode? _regionCode;

    public KartStorageSystemBuilder()
    {
        _useRho = false;
        _useRho5 = false;
        _usePackFolderListFile = false;
        _regionCode = null;
    }

    public KartStorageSystemBuilder UseRho()
    {
        _useRho = true;
        return this;
    }

    public KartStorageSystemBuilder UseRho5()
    {
        _useRho5 = true;
        return this;
    }

    public KartStorageSystemBuilder UsePackFolderListFile()
    {
        _usePackFolderListFile = true;
        return this;
    }

    public KartStorageSystemBuilder SetClientRegion(CountryCode regionCode)
    {
        _regionCode = regionCode;
        return this;
    }

    public KartStorageSystemBuilder SetClientPath(string kartriderClientPath)
    {
        _kartriderClientPath = kartriderClientPath;
        return this;
    }

    public KartStorageSystemBuilder SetDataPath(string kartriderDataPath)
    {
        _kartriderDataPath = kartriderDataPath;
        return this;
    }

    public KartStorageSystem Build()
    {
        return new KartStorageSystem(_useRho, _useRho5, _usePackFolderListFile, _regionCode, _kartriderClientPath, _kartriderDataPath);
    }
}