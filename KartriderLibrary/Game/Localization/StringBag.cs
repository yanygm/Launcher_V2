using KartLibrary.Consts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Game.Localization
{
    public class StringBag
    {
        private Dictionary<string, Dictionary<CountryCode, string>> _container = new Dictionary<string, Dictionary<CountryCode, string>>();
        
        public StringBag()
        {

        }

        public string GetString(CountryCode country, string key)
        {
            if (_container.ContainsKey(key))
                if (_container[key] is not null)
                    if (_container[key].ContainsKey(country))
                        return _container[key][country];
            return $"!sb({key})";
        }

        public void SetString(CountryCode country, string key, string value)
        {
            if (!_container.ContainsKey(key))
                _container.Add(key, new Dictionary<CountryCode, string>());
            if (_container[key] is null)
                _container[key] = new Dictionary<CountryCode, string>();
            if (!_container[key].ContainsKey(country))
                _container[key].Add(country, value);
            else
                _container[key][country] = value;
        }
    }
}
