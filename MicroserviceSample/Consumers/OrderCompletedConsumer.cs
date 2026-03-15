using MassTransit;
using MicroserviceSample.Contracts.Events;
using MicroserviceSample.Domain;
using MicroserviceSample.Infrastructure.Data;
using MicroserviceSample.Infrastructure.EventStore;

namespace MicroserviceSample.Consumers;

public class OrderCompletedConsumer(ApplicationDbContext db, IEventStore eventStore) : IConsumer<OrderCompletedEvent>
{
    public async Task Consume(ConsumeContext<OrderCompletedEvent> context)
    {
        var message = context.Message;

        var order = await db.Orders.FindAsync(message.OrderId);
        if (order is not null)
        {
            order.Status = OrderStatus.Completed;
            await db.SaveChangesAsync();
        }

        await eventStore.SaveEventAsync(message.CorrelationId, message);
    }
}
