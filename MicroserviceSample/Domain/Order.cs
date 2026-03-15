namespace MicroserviceSample.Domain
{
    public class Order
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Submitted;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Customer Customer { get; set; } = default!;
        public List<OrderItem> OrderItems { get; set; } = [];
    }
}
