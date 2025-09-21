using System;

namespace CorporateApp.Application.DTOs.User
{
    public class UserListDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{Name} {LastName}";
        public string Email { get; set; }
        public string Tcno { get; set; }
        public string Location { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } // Can be populated if you have a Role entity
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
