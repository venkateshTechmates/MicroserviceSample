using MicroserviceSample.Domain;
using MicroserviceSample.Infrastructure.Data;

namespace MicroserviceSample.CQRS.Commands;

public record CreateOrderCommand(int CustomerId, List<CreateOrderItemCommand> Items);
public record CreateOrderItemCommand(int ProductId, int Quantity, decimal UnitPrice);

public class OrderCommandHandler(ApplicationDbContext db)
{
    public async Task<Order> Handle(CreateOrderCommand command)
    {
        var order = new Order
        {
            CustomerId = command.CustomerId,
            Status = OrderStatus.Submitted,
            TotalAmount = command.Items.Sum(i => i.Quantity * i.UnitPrice),
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

        return order;
    }
}
