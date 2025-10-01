namespace AnomaliesDetector
{
    public record Anomaly(DateTime timestamp, string user_id, string ip_address, string reason, string mitigation);
}
