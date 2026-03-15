namespace MicroserviceSample.Domain
{
    public class Customer
    {
        public int CustomerId { get; set; }
        public string Name { get; set; } = default!;
        public string Email { get; set; } = default!;
        public List<Order> Orders { get; set; } = [];
    }
}
