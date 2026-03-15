namespace MicroserviceSample.Contracts.Events;

public record InventoryReservedEvent
{
    public Guid CorrelationId { get; init; }
    public int OrderId { get; init; }
    public bool IsReserved { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
