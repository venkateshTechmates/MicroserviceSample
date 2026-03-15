namespace MicroserviceSample.Domain;

public class StoredEvent
{
    public long Id { get; set; }
    public Guid CorrelationId { get; set; }
    public string EventType { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
