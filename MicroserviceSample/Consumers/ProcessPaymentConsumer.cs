using MassTransit;
using MicroserviceSample.Contracts.Commands;
using MicroserviceSample.Contracts.Events;
using MicroserviceSample.Domain;
using MicroserviceSample.Infrastructure.Data;
using MicroserviceSample.Infrastructure.EventStore;
using MicroserviceSample.Infrastructure.Messaging;

namespace MicroserviceSample.Consumers;

public class ProcessPaymentConsumer(ApplicationDbContext db, IEventStore eventStore) : IConsumer<ProcessPaymentCommand>
{
    public async Task Consume(ConsumeContext<ProcessPaymentCommand> context)
    {
        var command = context.Message;

        var payment = new Payment
        {
            OrderId = command.OrderId,
            Amount = command.Amount,
            IsSuccessful = true,
            ProcessedAt = DateTime.UtcNow
        };

        db.Payments.Add(payment);

        var order = await db.Orders.FindAsync(command.OrderId);
        if (order is not null)
            order.Status = OrderStatus.PaymentCompleted;

        await db.SaveChangesAsync();

        var @event = new PaymentProcessedEvent
        {
            CorrelationId = command.CorrelationId,
            OrderId = command.OrderId,
            PaymentId = payment.Id,
            IsSuccessful = true
        };

        await eventStore.SaveEventAsync(command.CorrelationId, @event);

        var sagaEndpoint = await context.GetSendEndpoint(new Uri($"queue:{QueueNames.OrderSaga}"));
        await sagaEndpoint.Send(@event);
    }
}
