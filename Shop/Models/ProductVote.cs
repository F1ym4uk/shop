namespace Shop.Models
{
    public class ProductVote
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public int Rating { get; set; }

        public Product Product { get; set; }
    }
}
