using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Shop.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Обязательное поле!")]
        [StringLength(100)]
        [Display(Name = "Логин")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Обязательное поле!")]
        [StringLength(100)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Обязательное поле!")]
        [Phone(ErrorMessage = "Введите корректный номер телефона")]
        [RegularExpression(@"^\+375\((29|33|44|25)\)\d{3}-\d{2}-\d{2}$", ErrorMessage = "Формат номера: +375(29/33/44/25)XXX-XX-XX")]
        [Display(Name = "Номер телефона")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Обязательное поле!")]
        [StringLength(100)]
        [Display(Name = "Электронная почта")]
        public string Email { get; set; }

        public bool IsAdmin { get; set; }
    }
}