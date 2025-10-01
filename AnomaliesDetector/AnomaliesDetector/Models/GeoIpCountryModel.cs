using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnomaliesDetector
{
    public class GeoIpCountryModel  // The class that represent a country that we get from the database.
    {
        public string IpAddress { get; }

        public string? CountryIsoCode { get; }

        public string? CountryName { get; }

        public GeoIpCountryModel(string ipAddress, string? countryIsoCode, string? countryName)
        {
            IpAddress = ipAddress;
            CountryIsoCode = countryIsoCode;
            CountryName = countryName;
        }
    }
}
