namespace AnomaliesDetector
{
    public interface IGeoIpService
    {
        GeoIpCountryModel? GetCountry(string? ipAddress);
    }
}
