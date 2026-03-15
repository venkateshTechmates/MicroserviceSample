namespace MicroserviceSample.Contracts.Events;

public record OrderSubmittedEvent
{
    public Guid CorrelationId { get; init; }
    public int OrderId { get; init; }
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = default!;
    public decimal TotalAmount { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
