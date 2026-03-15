using MassTransit;
using Microsoft.EntityFrameworkCore;
using MicroserviceSample.Contracts.Commands;
using MicroserviceSample.Contracts.Events;
using MicroserviceSample.Infrastructure.Data;
using MicroserviceSample.Infrastructure.EventStore;
using MicroserviceSample.Infrastructure.Messaging;

namespace MicroserviceSample.Consumers;

public class ReserveInventoryConsumer(ApplicationDbContext db, IEventStore eventStore) : IConsumer<ReserveInventoryCommand>
{
    public async Task Consume(ConsumeContext<ReserveInventoryCommand> context)
    {
        var command = context.Message;

        var items = command.Items.Count > 0
            ? command.Items
            : (await db.OrderItems
                .Where(oi => oi.OrderId == command.OrderId)
                .Select(oi => new OrderItemDto { ProductId = oi.ProductId, Quantity = oi.Quantity, UnitPrice = oi.UnitPrice })
                .ToListAsync());

        var allReserved = true;

        foreach (var item in items)
        {
            var inventory = await db.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.Quantity >= item.Quantity);

            if (inventory is not null)
                inventory.Quantity -= item.Quantity;
            else
            {
                allReserved = false;
                break;
            }
        }

        if (allReserved)
            await db.SaveChangesAsync();

        var order = await db.Orders.FindAsync(command.OrderId);
        if (order is not null)
        {
            order.Status = allReserved ? Domain.OrderStatus.InventoryReserved : Domain.OrderStatus.Faulted;
            await db.SaveChangesAsync();
        }

        var @event = new InventoryReservedEvent
        {
            CorrelationId = command.CorrelationId,
            OrderId = command.OrderId,
            IsReserved = allReserved
        };

        await eventStore.SaveEventAsync(command.CorrelationId, @event);

        var sagaEndpoint = await context.GetSendEndpoint(new Uri($"queue:{QueueNames.OrderSaga}"));
        await sagaEndpoint.Send(@event);
    }
}

