using System.Collections.Generic;

namespace CorporateApp.Application.DTOs.User
{
    public class UserListResponse
    {
        public IEnumerable<UserListDto> Users { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
}
