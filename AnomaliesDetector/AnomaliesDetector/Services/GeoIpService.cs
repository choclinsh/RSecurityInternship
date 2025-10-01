using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Exceptions;
using MaxMind.GeoIP2.Responses;

namespace AnomaliesDetector
{
    public class GeoIpService : IGeoIpService, IDisposable
    {
        private readonly DatabaseReader _countryDatabaseReader;
        const string IP_ADDRESS_REGEX = "^(?:1)?(?:\\d{1,2}|2(?:[0-4]\\d|5[0-5]))\\.(?:1)?(?:\\d{1,2}|2(?:[0-4]\\d|5[0-5]))\\.(?:1)?(?:\\d{1,2}|2(?:[0-4]\\d|5[0-5]))\\.(?:1)?(?:\\d{1,2}|2(?:[0-4]\\d|5[0-5]))$";

        public GeoIpService()
        {// connect to the GeoLite2-Country db
            _countryDatabaseReader = new DatabaseReader(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "resources/GeoLite2-Country.mmdb"));
        }

        public GeoIpCountryModel? GetCountry(string? ipAdress)
        {
            if (!IsValidIpAddress(ipAdress))
            {
                return null;
            }
            CountryResponse countryResponse;
            try
            {
                countryResponse = _countryDatabaseReader.Country(ipAdress);  // perform the querry to the databse
            }
            catch (AddressNotFoundException)
            {
                return null;
            }
            if (countryResponse?.Country == null)
            {
                return null;
            }
            return new GeoIpCountryModel(ipAdress, countryResponse.Country.IsoCode,  // return the country name with adress and code
                countryResponse.Country.Name);
        }

        private bool IsValidIpAddress(string? ipAddress)
        {
            // Don't check if there is no IP address.
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return false;
            }

            // Check format of IP address
            if (!Regex.IsMatch(ipAddress, IP_ADDRESS_REGEX))
            {
                return false;
            }

            return true;
        }

        public void Dispose(bool disposing) { }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
