namespace CorporateApp.Application.DTOs
{
    public class CreateUserDto
    {
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Tcno { get; set; }
        public string Location { get; set; }
        public int RoleId { get; set; }
    }
}
