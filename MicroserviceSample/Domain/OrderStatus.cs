namespace MicroserviceSample.Domain;

public enum OrderStatus
{
    Submitted,
    PaymentPending,
    PaymentCompleted,
    InventoryReserved,
    Completed,
    Faulted
}
