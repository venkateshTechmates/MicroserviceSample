namespace MicroserviceSample.Contracts.Commands;

public record SubmitOrderCommand
{
    public Guid CorrelationId { get; init; }
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = default!;
    public decimal TotalAmount { get; init; }
    public List<OrderItemDto> Items { get; init; } = [];
}

public record OrderItemDto
{
    public int ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}
