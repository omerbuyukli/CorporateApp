using CorporateApp.Application.DTOs.Auth;
using CorporateApp.Application.Interfaces;
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
            // TODO: User authentication logic
            // Şimdilik test için sabit değerler

            if (loginDto.Email == "test" && loginDto.Password == "test")
            {
                var token = _tokenService.GenerateToken(
                    userId: "1",
                    email: loginDto.Email,
                    roles: new List<string> { "Admin", "User" }
                );

                return new TokenResponseDto
                {
                    Token = token,
                    Expiration = DateTime.UtcNow.AddMinutes(60)
                };
            }

            throw new UnauthorizedAccessException("Invalid credentials");
        }

        public async Task<TokenResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            // TODO: User registration logic
            throw new NotImplementedException();
        }
    }
}