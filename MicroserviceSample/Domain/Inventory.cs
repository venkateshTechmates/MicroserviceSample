namespace MicroserviceSample.Domain
{
    public class Inventory
    {
        public int Id { get; set; }
        public string Sku { get; set; } = default!;
        public int Quantity { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } = default!;
    }
}
