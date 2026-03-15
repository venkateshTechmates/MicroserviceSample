namespace MicroserviceSample.Contracts.Commands;

public record ProcessPaymentCommand
{
    public Guid CorrelationId { get; init; }
    public int OrderId { get; init; }
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = default!;
    public decimal Amount { get; init; }
}
