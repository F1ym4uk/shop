using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shop.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Обязательное поле!")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Обязательное поле!")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Обязательное поле!")]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Обязательное поле!")]
        public string Image { get; set; } = string.Empty;

        [Required(ErrorMessage = "Обязательное поле!")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Обязательное поле!")]
        [Range(0, int.MaxValue, ErrorMessage = "Количество товаров не может быть отрицательным")]
        public int StockQuantity { get; set; }

        [Range(0, 5)]
        public double AverageRating { get; set; } = 0;

        public int TotalVotes { get; set; } = 0;
    }
}
