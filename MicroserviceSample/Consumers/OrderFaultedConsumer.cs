using MassTransit;
using MicroserviceSample.Contracts.Events;
using MicroserviceSample.Domain;
using MicroserviceSample.Infrastructure.Data;
using MicroserviceSample.Infrastructure.EventStore;

namespace MicroserviceSample.Consumers;

public class OrderFaultedConsumer(ApplicationDbContext db, IEventStore eventStore) : IConsumer<OrderFaultedEvent>
{
    public async Task Consume(ConsumeContext<OrderFaultedEvent> context)
    {
        var message = context.Message;

        var order = await db.Orders.FindAsync(message.OrderId);
        if (order is not null)
        {
            order.Status = OrderStatus.Faulted;
            await db.SaveChangesAsync();
        }

        await eventStore.SaveEventAsync(message.CorrelationId, message);
    }
}
