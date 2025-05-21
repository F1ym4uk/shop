using System.ComponentModel.DataAnnotations;

namespace Shop.ViewModels
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Обязательное поле!")]
        [StringLength(100, ErrorMessage = "Логин должен быть от 3 до 100 символов", MinimumLength = 3)]
        [Display(Name = "Логин")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Обязательное поле!")]
        [Phone(ErrorMessage = "Введите корректный номер телефона")]
        [RegularExpression(@"^\+375\((29|33|44|25)\)\d{3}-\d{2}-\d{2}$",
            ErrorMessage = "Формат номера: +375(29/33/44/25)XXX-XX-XX")]
        [Display(Name = "Номер телефона")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Обязательное поле!")]
        [EmailAddress(ErrorMessage = "Введите корректный email")]
        [Display(Name = "Электронная почта")]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Новый пароль (оставьте пустым, если не хотите менять)")]
        [StringLength(100, ErrorMessage = "Пароль должен быть от 6 символов", MinimumLength = 5)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Подтвердите текущий пароль")]
        [Required(ErrorMessage = "Необходимо подтвердить текущий пароль")]
        public string CurrentPassword { get; set; }
    }
}