using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MicroserviceSample.Sagas;

public class OrderSagaMap : SagaClassMap<OrderSagaState>
{
    protected override void Configure(EntityTypeBuilder<OrderSagaState> entity, ModelBuilder model)
    {
        entity.ToTable("OrderSagaStates");
        entity.Property(x => x.CurrentState).HasMaxLength(64);
        entity.Property(x => x.CustomerName).HasMaxLength(256);
        entity.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
    }
}
