namespace MicroserviceSample.Contracts.Events;

public record PaymentProcessedEvent
{
    public Guid CorrelationId { get; init; }
    public int OrderId { get; init; }
    public int PaymentId { get; init; }
    public bool IsSuccessful { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
