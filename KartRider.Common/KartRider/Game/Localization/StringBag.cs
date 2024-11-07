using System.Collections.Generic;
using KartLibrary.Consts;

namespace KartLibrary.Game.Localization;

public class StringBag
{
    private Dictionary<string, Dictionary<CountryCode, string>> _container = new Dictionary<string, Dictionary<CountryCode, string>>();

    public string GetString(CountryCode country, string key)
    {
        if (_container.ContainsKey(key) && _container[key] != null && _container[key].ContainsKey(country))
        {
            return _container[key][country];
        }

        return "!sb(" + key + ")";
    }

    public void SetString(CountryCode country, string key, string value)
    {
        if (!_container.ContainsKey(key))
        {
            _container.Add(key, new Dictionary<CountryCode, string>());
        }

        if (_container[key] == null)
        {
            _container[key] = new Dictionary<CountryCode, string>();
        }

        if (!_container[key].ContainsKey(country))
        {
            _container[key].Add(country, value);
        }
        else
        {
            _container[key][country] = value;
        }
    }
}