using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using MicroserviceSample.Domain;
using MicroserviceSample.Sagas;

namespace MicroserviceSample.Infrastructure.Data
{
    public class ApplicationDbContext : SagaDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Inventory> Inventories => Set<Inventory>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<StoredEvent> StoredEvents => Set<StoredEvent>();

        protected override IEnumerable<ISagaClassMap> Configurations
        {
            get { yield return new OrderSagaMap(); }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(c => c.CustomerId);
                entity.Property(c => c.Name).HasMaxLength(200).IsRequired();
                entity.Property(c => c.Email).HasMaxLength(200).IsRequired();
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
                entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.OrderId);
                entity.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(o => o.Status).HasConversion<string>().HasMaxLength(50);
                entity.HasOne(o => o.Customer).WithMany(c => c.Orders).HasForeignKey(o => o.CustomerId);
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(oi => oi.Id);
                entity.Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)");
                entity.HasOne(oi => oi.Order).WithMany(o => o.OrderItems).HasForeignKey(oi => oi.OrderId);
                entity.HasOne(oi => oi.Product).WithMany().HasForeignKey(oi => oi.ProductId);
            });

            modelBuilder.Entity<Inventory>(entity =>
            {
                entity.HasKey(i => i.Id);
                entity.Property(i => i.Sku).HasMaxLength(100).IsRequired();
                entity.HasOne(i => i.Product).WithMany(p => p.Inventories).HasForeignKey(i => i.ProductId);
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");
                entity.HasOne(p => p.Order).WithMany().HasForeignKey(p => p.OrderId);
            });

            modelBuilder.Entity<StoredEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EventType).HasMaxLength(256).IsRequired();
                entity.HasIndex(e => e.CorrelationId);
            });

            SeedData(modelBuilder);
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>().HasData(
                new Customer { CustomerId = 1, Name = "John Doe", Email = "john@example.com" },
                new Customer { CustomerId = 2, Name = "Jane Smith", Email = "jane@example.com" },
                new Customer { CustomerId = 3, Name = "Bob Wilson", Email = "bob@example.com" }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Laptop", Description = "High-performance laptop", Price = 999.99m },
                new Product { Id = 2, Name = "Mouse", Description = "Wireless mouse", Price = 29.99m },
                new Product { Id = 3, Name = "Keyboard", Description = "Mechanical keyboard", Price = 79.99m },
                new Product { Id = 4, Name = "Monitor", Description = "27-inch 4K monitor", Price = 499.99m },
                new Product { Id = 5, Name = "Headset", Description = "Noise-cancelling headset", Price = 149.99m }
            );

            modelBuilder.Entity<Inventory>().HasData(
                new Inventory { Id = 1, ProductId = 1, Sku = "LAP-001", Quantity = 50 },
                new Inventory { Id = 2, ProductId = 2, Sku = "MOU-001", Quantity = 200 },
                new Inventory { Id = 3, ProductId = 3, Sku = "KEY-001", Quantity = 150 },
                new Inventory { Id = 4, ProductId = 4, Sku = "MON-001", Quantity = 30 },
                new Inventory { Id = 5, ProductId = 5, Sku = "HDS-001", Quantity = 100 }
            );

            modelBuilder.Entity<Order>().HasData(
                new Order { OrderId = 1, CustomerId = 1, TotalAmount = 1029.98m, Status = OrderStatus.Completed, CreatedAt = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc) },
                new Order { OrderId = 2, CustomerId = 2, TotalAmount = 499.99m, Status = OrderStatus.Completed, CreatedAt = new DateTime(2025, 2, 10, 0, 0, 0, DateTimeKind.Utc) }
            );

            modelBuilder.Entity<OrderItem>().HasData(
                new OrderItem { Id = 1, OrderId = 1, ProductId = 1, Quantity = 1, UnitPrice = 999.99m },
                new OrderItem { Id = 2, OrderId = 1, ProductId = 2, Quantity = 1, UnitPrice = 29.99m },
                new OrderItem { Id = 3, OrderId = 2, ProductId = 4, Quantity = 1, UnitPrice = 499.99m }
            );

            modelBuilder.Entity<Payment>().HasData(
                new Payment { Id = 1, OrderId = 1, Amount = 1029.98m, IsSuccessful = true, ProcessedAt = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc) },
                new Payment { Id = 2, OrderId = 2, Amount = 499.99m, IsSuccessful = true, ProcessedAt = new DateTime(2025, 2, 10, 0, 0, 0, DateTimeKind.Utc) }
            );
        }
    }
}
