using System;

namespace CorporateApp.Core.Entities
{
    public class User : BaseEntity
    {
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }  // Password property
        public string Tcno { get; set; }
        public string Location { get; set; }
        public int RoleId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }  // CreatedDate property
        public DateTime? UpdatedDate { get; set; }
    }
}
