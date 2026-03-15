using MassTransit;
using MicroserviceSample.Contracts.Commands;
using MicroserviceSample.Contracts.Events;
using MicroserviceSample.Domain;
using MicroserviceSample.Infrastructure.Data;
using MicroserviceSample.Infrastructure.EventStore;
using MicroserviceSample.Infrastructure.Messaging;

namespace MicroserviceSample.Consumers;

public class SubmitOrderConsumer(ApplicationDbContext db, IEventStore eventStore) : IConsumer<SubmitOrderCommand>
{
    public async Task Consume(ConsumeContext<SubmitOrderCommand> context)
    {
        var command = context.Message;

        var order = new Order
        {
            CustomerId = command.CustomerId,
            TotalAmount = command.TotalAmount,
            Status = OrderStatus.Submitted,
            CreatedAt = DateTime.UtcNow,
            OrderItems = command.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var @event = new OrderSubmittedEvent
        {
            CorrelationId = command.CorrelationId,
            OrderId = order.OrderId,
            CustomerId = command.CustomerId,
            CustomerName = command.CustomerName,
            TotalAmount = command.TotalAmount
        };

        await eventStore.SaveEventAsync(command.CorrelationId, @event);

        var sagaEndpoint = await context.GetSendEndpoint(new Uri($"queue:{QueueNames.OrderSaga}"));
        await sagaEndpoint.Send(@event);
    }
}
