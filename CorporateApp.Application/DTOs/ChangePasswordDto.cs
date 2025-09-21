using System.ComponentModel.DataAnnotations;

namespace CorporateApp.Application.DTOs.User
{
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Mevcut şifre gereklidir")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Yeni şifre gereklidir")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Şifre tekrarı gereklidir")]
        [Compare("NewPassword", ErrorMessage = "Şifreler uyuşmuyor")]
        public string ConfirmPassword { get; set; }
    }

    public class ChangePasswordResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}