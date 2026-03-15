namespace MicroserviceSample.Contracts.Events;

public record OrderFaultedEvent
{
    public Guid CorrelationId { get; init; }
    public int OrderId { get; init; }
    public string Reason { get; init; } = default!;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
