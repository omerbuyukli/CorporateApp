namespace CorporateApp.Application.Interfaces
{
    public interface IDiaLoginService
    {
        Task<(bool Success, string SessionId, string Message)> LoginAsync();
    }
}