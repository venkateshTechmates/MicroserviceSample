namespace MicroserviceSample.Domain
{
    public class Payment
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public bool IsSuccessful { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public Order Order { get; set; } = default!;
    }
}
