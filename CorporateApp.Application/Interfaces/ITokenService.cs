namespace CorporateApp.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(string userId, string email, List<string> roles);
    }
}