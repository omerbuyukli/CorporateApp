namespace CorporateApp.Core.Entities.DiaEntities
{
    public class Login
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string DisconnectSameUser { get; set; }
        public string Lang { get; set; }
        public Params Params { get; set; }
    }
}