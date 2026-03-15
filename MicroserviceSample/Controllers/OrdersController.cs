using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MicroserviceSample.Contracts.Commands;
using MicroserviceSample.CQRS.Queries;
using MicroserviceSample.Infrastructure.Data;
using MicroserviceSample.Infrastructure.EventStore;
using MicroserviceSample.Infrastructure.Messaging;
using MicroserviceSample.Sagas;

namespace MicroserviceSample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(
    OrderQueryHandler queryHandler,
    ISendEndpointProvider sendEndpointProvider,
    IEventStore eventStore,
    ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var orders = await queryHandler.Handle(new GetAllOrdersQuery());
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await queryHandler.Handle(new GetOrderByIdQuery(id));
        return order is null ? NotFound() : Ok(order);
    }

    [HttpGet("customer/{customerId}")]
    public async Task<IActionResult> GetByCustomer(int customerId)
    {
        var orders = await queryHandler.Handle(new GetCustomerOrdersQuery(customerId));
        return Ok(orders);
    }

    /// <summary>
    /// Submit a new order — triggers the full Saga: SubmitOrder → ProcessPayment → ReserveInventory → Complete
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SubmitOrder([FromBody] CreateOrderRequest request)
    {
        var correlationId = Guid.NewGuid();

        var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{QueueNames.SubmitOrder}"));
        await endpoint.Send(new SubmitOrderCommand
        {
            CorrelationId = correlationId,
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName ?? "Customer",
            TotalAmount = request.Items.Sum(i => i.Quantity * i.UnitPrice),
            Items = request.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        });

        return Accepted(new
        {
            CorrelationId = correlationId,
            Message = "Order accepted. Processing via saga: Payment → Inventory → Complete."
        });
    }

    /// <summary>
    /// Get the current saga state for a given correlationId
    /// </summary>
    [HttpGet("saga/{correlationId}")]
    public async Task<IActionResult> GetSagaState(Guid correlationId)
    {
        var state = await db.Set<OrderSagaState>()
            .FirstOrDefaultAsync(s => s.CorrelationId == correlationId);

        return state is null ? NotFound() : Ok(state);
    }

    /// <summary>
    /// Get all stored events for a given correlationId
    /// </summary>
    [HttpGet("events/{correlationId}")]
    public async Task<IActionResult> GetEvents(Guid correlationId)
    {
        var events = await eventStore.GetEventsAsync(correlationId);
        return Ok(events);
    }
}

public record CreateOrderRequest
{
    public int CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public List<CreateOrderItemRequest> Items { get; init; } = [];
}

public record CreateOrderItemRequest
{
    public int ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}
