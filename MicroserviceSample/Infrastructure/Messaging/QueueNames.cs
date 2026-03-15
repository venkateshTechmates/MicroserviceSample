namespace MicroserviceSample.Infrastructure.Messaging;

public static class QueueNames
{
    public const string SubmitOrder = "submit-order";
    public const string ProcessPayment = "process-payment";
    public const string ReserveInventory = "reserve-inventory";
    public const string OrderCompleted = "order-completed";
    public const string OrderFaulted = "order-faulted";
    public const string OrderSaga = "order-saga";
}
