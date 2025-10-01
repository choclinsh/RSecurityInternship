using System;
using System.IO;
using System.Net;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Responses;

namespace AnomaliesDetector
{
    internal static class Geo
    {
        private static readonly object _initLock = new();  // mutex that defend the creation of reader object in this singelton pattern
        private static IGeoIpService _geoIpService;

        private static IGeoIpService Reader
        {
            get
            {
                if (_geoIpService != null) return _geoIpService;  // if created already return the object
                lock (_initLock)
                {
                    if (_geoIpService == null)  // create the reader only one
                    {
                        _geoIpService = new GeoIpService();
                    }
                }
                return _geoIpService!;
            }
        }

        public static GeoIpCountryModel? LookupCountryCode(IPAddress ip)
        {
            try
            {

                GeoIpCountryModel resp = Reader.GetCountry(ip.ToString());  // make the querry
                return resp ?? null;
            }
            catch
            {
                // Unknown or not found
                return null;
            }
        }

    }
}
