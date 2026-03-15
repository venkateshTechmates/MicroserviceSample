namespace MicroserviceSample.Contracts.Events;

public record OrderCompletedEvent
{
    public Guid CorrelationId { get; init; }
    public int OrderId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
