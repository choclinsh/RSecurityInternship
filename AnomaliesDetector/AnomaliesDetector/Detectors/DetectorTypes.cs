namespace AnomaliesDetector
{
    public delegate IEnumerable<Anomaly> Detector(LogRow[] rows, CancellationToken ct);
}
