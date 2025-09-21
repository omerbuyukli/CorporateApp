namespace CorporateApp.Infrastructure.Configuration
{
    public class DiaApiConfiguration
    {
        public string BaseUrl { get; set; } = string.Empty;
        public Dictionary<string, string> Endpoints { get; set; } = new();
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public int Timeout { get; set; } = 30;
    }
}