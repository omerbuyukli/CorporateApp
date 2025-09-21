namespace CorporateApp.Application.DTOs.User
{
    public class GetUsersListRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SearchTerm { get; set; }
        public int? RoleId { get; set; }
        public bool? IsActive { get; set; }
    }
}
