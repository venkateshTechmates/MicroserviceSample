using Azure.Messaging.ServiceBus.Administration;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using MicroserviceSample.Consumers;
using MicroserviceSample.CQRS.Commands;
using MicroserviceSample.CQRS.Queries;
using MicroserviceSample.Infrastructure.Data;
using MicroserviceSample.Infrastructure.EventStore;
using MicroserviceSample.Infrastructure.Messaging;
using MicroserviceSample.Sagas;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddOpenApi();

// Entity Framework Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

// CQRS Handlers
builder.Services.AddScoped<OrderCommandHandler>();
builder.Services.AddScoped<OrderQueryHandler>();

// Event Store
builder.Services.AddScoped<IEventStore, EventStore>();

// MassTransit — provider switched via MessageBroker:Provider in appsettings
var brokerProvider = builder.Configuration["MessageBroker:Provider"] ?? "RabbitMQ";

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SubmitOrderConsumer>();
    x.AddConsumer<ProcessPaymentConsumer>();
    x.AddConsumer<ReserveInventoryConsumer>();
    x.AddConsumer<OrderCompletedConsumer>();
    x.AddConsumer<OrderFaultedConsumer>();

    x.AddSagaStateMachine<OrderSaga, OrderSagaState>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
            r.AddDbContext<DbContext, ApplicationDbContext>((_, optionsBuilder) =>
            {
                optionsBuilder.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions => sqlOptions.EnableRetryOnFailure());
            });
        });

    if (brokerProvider.Equals("AzureServiceBus", StringComparison.OrdinalIgnoreCase))
    {
        // Azure Service Bus Basic tier — queues only, no topics
        x.UsingAzureServiceBus((context, cfg) =>
        {
            cfg.Host(builder.Configuration["AzureServiceBus:ConnectionString"]);

            ConfigureAzureQueueEndpoints(context, cfg);
        });
    }
    else
    {
        // RabbitMQ — same explicit queue routing keeps both transports consistent
        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(builder.Configuration["RabbitMq:Host"], "/", h =>
            {
                h.Username(builder.Configuration["RabbitMq:Username"]!);
                h.Password(builder.Configuration["RabbitMq:Password"]!);
            });
            ConfigureQueueEndpoints(context, cfg);
        });
    }
});

// Azure Service Bus Basic tier: no topics, no _error/_skipped queues, use built-in DLQ
static void ConfigureAzureQueueEndpoints(IRegistrationContext context, IServiceBusBusFactoryConfigurator cfg)
{
    cfg.ReceiveEndpoint(QueueNames.SubmitOrder, (IServiceBusReceiveEndpointConfigurator e) =>
    {
        e.ConfigureConsumeTopology = false;
        e.ConfigureDeadLetterQueueDeadLetterTransport();
        e.ConfigureDeadLetterQueueErrorTransport();
        e.ConfigureConsumer<SubmitOrderConsumer>(context);
    });

    cfg.ReceiveEndpoint(QueueNames.ProcessPayment, (IServiceBusReceiveEndpointConfigurator e) =>
    {
        e.ConfigureConsumeTopology = false;
        e.ConfigureDeadLetterQueueDeadLetterTransport();
        e.ConfigureDeadLetterQueueErrorTransport();
        e.ConfigureConsumer<ProcessPaymentConsumer>(context);
    });

    cfg.ReceiveEndpoint(QueueNames.ReserveInventory, (IServiceBusReceiveEndpointConfigurator e) =>
    {
        e.ConfigureConsumeTopology = false;
        e.ConfigureDeadLetterQueueDeadLetterTransport();
        e.ConfigureDeadLetterQueueErrorTransport();
        e.ConfigureConsumer<ReserveInventoryConsumer>(context);
    });

    cfg.ReceiveEndpoint(QueueNames.OrderCompleted, (IServiceBusReceiveEndpointConfigurator e) =>
    {
        e.ConfigureConsumeTopology = false;
        e.ConfigureDeadLetterQueueDeadLetterTransport();
        e.ConfigureDeadLetterQueueErrorTransport();
        e.ConfigureConsumer<OrderCompletedConsumer>(context);
    });

    cfg.ReceiveEndpoint(QueueNames.OrderFaulted, (IServiceBusReceiveEndpointConfigurator e) =>
    {
        e.ConfigureConsumeTopology = false;
        e.ConfigureDeadLetterQueueDeadLetterTransport();
        e.ConfigureDeadLetterQueueErrorTransport();
        e.ConfigureConsumer<OrderFaultedConsumer>(context);
    });

    cfg.ReceiveEndpoint(QueueNames.OrderSaga, (IServiceBusReceiveEndpointConfigurator e) =>
    {
        e.ConfigureConsumeTopology = false;
        e.ConfigureDeadLetterQueueDeadLetterTransport();
        e.ConfigureDeadLetterQueueErrorTransport();
        e.ConfigureSaga<OrderSagaState>(context);
    });
}

// RabbitMQ: standard queue endpoints
static void ConfigureQueueEndpoints(IRegistrationContext context, IBusFactoryConfigurator cfg)
{
    cfg.ReceiveEndpoint(QueueNames.SubmitOrder,
        e => e.ConfigureConsumer<SubmitOrderConsumer>(context));

    cfg.ReceiveEndpoint(QueueNames.ProcessPayment,
        e => e.ConfigureConsumer<ProcessPaymentConsumer>(context));

    cfg.ReceiveEndpoint(QueueNames.ReserveInventory,
        e => e.ConfigureConsumer<ReserveInventoryConsumer>(context));

    cfg.ReceiveEndpoint(QueueNames.OrderCompleted,
        e => e.ConfigureConsumer<OrderCompletedConsumer>(context));

    cfg.ReceiveEndpoint(QueueNames.OrderFaulted,
        e => e.ConfigureConsumer<OrderFaultedConsumer>(context));

    cfg.ReceiveEndpoint(QueueNames.OrderSaga,
        e => e.ConfigureSaga<OrderSagaState>(context));
}

var app = builder.Build();

// Azure Service Bus Basic tier: queues must be pre-created (SDK cannot create them on Basic tier)
// Run Scripts/setup-azure-servicebus-queues.ps1 to create them
if (brokerProvider.Equals("AzureServiceBus", StringComparison.OrdinalIgnoreCase))
{
    var asbConnStr = app.Configuration["AzureServiceBus:ConnectionString"]!;
    var adminClient = new ServiceBusAdministrationClient(asbConnStr);

    string[] requiredQueues = [
        QueueNames.SubmitOrder, QueueNames.ProcessPayment, QueueNames.ReserveInventory,
        QueueNames.OrderCompleted, QueueNames.OrderFaulted, QueueNames.OrderSaga
    ];

    foreach (var queue in requiredQueues)
    {
        if (!await adminClient.QueueExistsAsync(queue))
        {
            Console.WriteLine($"⚠️  Queue '{queue}' missing. Run: Scripts/setup-azure-servicebus-queues.ps1");
        }
    }
}

// Auto-migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Order Processing Microservice API");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
