namespace MicroserviceSample.Contracts.Commands;

public record ReserveInventoryCommand
{
    public Guid CorrelationId { get; init; }
    public int OrderId { get; init; }
    public List<OrderItemDto> Items { get; init; } = [];
}
