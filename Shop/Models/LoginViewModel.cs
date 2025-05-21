using System.ComponentModel.DataAnnotations;

namespace Shop.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Обязательное поле!")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Обязательное поле!")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

}
