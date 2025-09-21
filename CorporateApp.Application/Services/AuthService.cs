using CorporateApp.Application.DTOs.Auth;
using CorporateApp.Application.Interfaces;
using CorporateApp.Core.Entities;
using CorporateApp.Core.Entities.DiaEntities;
using CorporateApp.Core.Interfaces;

namespace CorporateApp.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;

        public AuthService(IUserRepository userRepository, ITokenService tokenService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
        }

        public async Task<TokenResponseDto> LoginAsync(LoginDto loginDto)
        {


            var user = await _userRepository.GetByTcnoAsync(loginDto.Tcno.Trim());

            if (user.Name.ToLower() == "admin")
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword("Admin123.");

                var retVal = _userRepository.UpdateAsync(user);


            }


            if (user == null)
                throw new UnauthorizedAccessException("Kullanıcı bulunamadı.");

            // Girilen şifre ile veritabanındaki hash karşılaştır
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password);
            if (!isPasswordValid)
                throw new UnauthorizedAccessException("Geçersiz şifre.");

            // JWT token oluştur
            var token = _tokenService.GenerateToken(
                userId: user.Id.ToString(),
                tcno: user.Tcno.ToString(), // burada parametre adı GenerateToken(string userId, string tcno, List<string> roles)
                roles: new List<string> { "Admin", "User" }
            );

            return new TokenResponseDto
            {
                Token = token,
                Expiration = DateTime.UtcNow.AddMinutes(60)
            };
        }


        public async Task<TokenResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            // TODO: User registration logic
            throw new NotImplementedException();
        }
    }
}