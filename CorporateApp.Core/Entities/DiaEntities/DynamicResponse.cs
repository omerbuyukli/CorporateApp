namespace CorporateApp.Core.Entities.DiaEntities
{
    public class DynamicResponse
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string SessionId { get; set; }
        public dynamic Data { get; set; }
    }
}