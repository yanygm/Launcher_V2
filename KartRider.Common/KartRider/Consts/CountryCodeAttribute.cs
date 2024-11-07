using System;

namespace KartLibrary.Consts;

[AttributeUsage(AttributeTargets.Field)]
public class CountryCodeAttribute : Attribute
{
    public string CountryName { get; set; }

    public CountryCodeAttribute(string countryName)
    {
        CountryName = countryName;
    }
}