using System.ComponentModel.DataAnnotations;

namespace CorporateApp.Application.DTOs.User
{
    public class AdminChangePasswordDto
    {
        [Required(ErrorMessage = "Yeni şifre gereklidir")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Şifre tekrarı gereklidir")]
        [Compare("NewPassword", ErrorMessage = "Şifreler uyuşmuyor")]
        public string ConfirmPassword { get; set; }
    }
}