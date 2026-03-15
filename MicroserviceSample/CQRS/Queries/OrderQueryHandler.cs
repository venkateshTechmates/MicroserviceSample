using Microsoft.EntityFrameworkCore;
using MicroserviceSample.Domain;
using MicroserviceSample.Infrastructure.Data;

namespace MicroserviceSample.CQRS.Queries;

public record GetOrderByIdQuery(int OrderId);
public record GetAllOrdersQuery;
public record GetCustomerOrdersQuery(int CustomerId);

public class OrderQueryHandler(ApplicationDbContext db)
{
    public async Task<Order?> Handle(GetOrderByIdQuery query)
    {
        return await db.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.OrderId == query.OrderId);
    }

    public async Task<List<Order>> Handle(GetAllOrdersQuery query)
    {
        return await db.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .ToListAsync();
    }

    public async Task<List<Order>> Handle(GetCustomerOrdersQuery query)
    {
        return await db.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.CustomerId == query.CustomerId)
            .ToListAsync();
    }
}
