namespace AnomaliesDetector
{
    public record LogRow(DateTime timestamp, string user_id, string action, string ip_address);
}
