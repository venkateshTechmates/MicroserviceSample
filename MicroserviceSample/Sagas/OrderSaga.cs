using MassTransit;
using MicroserviceSample.Contracts.Commands;
using MicroserviceSample.Contracts.Events;
using MicroserviceSample.Infrastructure.Messaging;

namespace MicroserviceSample.Sagas;

public class OrderSaga : MassTransitStateMachine<OrderSagaState>
{
    public State PaymentPending { get; private set; } = default!;
    public State InventoryPending { get; private set; } = default!;
    public State Completed { get; private set; } = default!;
    public State Faulted { get; private set; } = default!;

    public Event<OrderSubmittedEvent> OrderSubmitted { get; private set; } = default!;
    public Event<PaymentProcessedEvent> PaymentProcessed { get; private set; } = default!;
    public Event<InventoryReservedEvent> InventoryReserved { get; private set; } = default!;

    public OrderSaga()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderSubmitted, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => PaymentProcessed, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => InventoryReserved, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

        Initially(
            When(OrderSubmitted)
                .Then(context =>
                {
                    context.Saga.OrderId = context.Message.OrderId;
                    context.Saga.CustomerId = context.Message.CustomerId;
                    context.Saga.CustomerName = context.Message.CustomerName;
                    context.Saga.TotalAmount = context.Message.TotalAmount;
                    context.Saga.CreatedAt = DateTime.UtcNow;
                })
                .ThenAsync(async context =>
                {
                    var endpoint = await context.GetSendEndpoint(new Uri($"queue:{QueueNames.ProcessPayment}"));
                    await endpoint.Send<ProcessPaymentCommand>(new
                    {
                        context.Saga.CorrelationId,
                        context.Saga.OrderId,
                        context.Saga.CustomerId,
                        context.Saga.CustomerName,
                        Amount = context.Saga.TotalAmount
                    });
                })
                .TransitionTo(PaymentPending)
        );

        During(PaymentPending,
            When(PaymentProcessed, ctx => ctx.Message.IsSuccessful)
                .Then(context => context.Saga.UpdatedAt = DateTime.UtcNow)
                .ThenAsync(async context =>
                {
                    var endpoint = await context.GetSendEndpoint(new Uri($"queue:{QueueNames.ReserveInventory}"));
                    await endpoint.Send<ReserveInventoryCommand>(new
                    {
                        context.Saga.CorrelationId,
                        context.Saga.OrderId,
                        Items = Array.Empty<OrderItemDto>()
                    });
                })
                .TransitionTo(InventoryPending),

            When(PaymentProcessed, ctx => !ctx.Message.IsSuccessful)
                .Then(context => context.Saga.UpdatedAt = DateTime.UtcNow)
                .ThenAsync(async context =>
                {
                    var endpoint = await context.GetSendEndpoint(new Uri($"queue:{QueueNames.OrderFaulted}"));
                    await endpoint.Send<OrderFaultedEvent>(new
                    {
                        context.Saga.CorrelationId,
                        context.Saga.OrderId,
                        Reason = "Payment failed"
                    });
                })
                .TransitionTo(Faulted)
        );

        During(InventoryPending,
            When(InventoryReserved, ctx => ctx.Message.IsReserved)
                .Then(context => context.Saga.UpdatedAt = DateTime.UtcNow)
                .ThenAsync(async context =>
                {
                    var endpoint = await context.GetSendEndpoint(new Uri($"queue:{QueueNames.OrderCompleted}"));
                    await endpoint.Send<OrderCompletedEvent>(new
                    {
                        context.Saga.CorrelationId,
                        context.Saga.OrderId
                    });
                })
                .TransitionTo(Completed)
                .Finalize(),

            When(InventoryReserved, ctx => !ctx.Message.IsReserved)
                .Then(context => context.Saga.UpdatedAt = DateTime.UtcNow)
                .ThenAsync(async context =>
                {
                    var endpoint = await context.GetSendEndpoint(new Uri($"queue:{QueueNames.OrderFaulted}"));
                    await endpoint.Send<OrderFaultedEvent>(new
                    {
                        context.Saga.CorrelationId,
                        context.Saga.OrderId,
                        Reason = "Inventory reservation failed"
                    });
                })
                .TransitionTo(Faulted)
        );

        SetCompletedWhenFinalized();
    }
}
