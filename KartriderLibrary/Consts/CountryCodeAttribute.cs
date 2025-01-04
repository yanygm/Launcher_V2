using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KartLibrary.Consts
{
    [AttributeUsage(AttributeTargets.Field)]
    public class CountryCodeAttribute : Attribute
    {
        public string CountryName { get; set; }

        public CountryCodeAttribute(string countryName)
        {
            CountryName = countryName;
        }
    }
}