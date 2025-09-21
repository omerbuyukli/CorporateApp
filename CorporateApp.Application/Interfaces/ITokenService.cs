namespace CorporateApp.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(string userId, string tcno, List<string> roles);
    }
}