using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CorporateApp.Core.Entities
{
    public class User : BaseEntity
    {
        [Column("FirstName")]
        [Required]
        [MaxLength(100)]
        public string? Name { get; set; }

        [Column("LastName")]
        [Required]
        [MaxLength(100)]
        public string? LastName { get; set; }

        [Column("Email")]
        [Required]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Column("PasswordHash")]
        [Required]
        public string? Password { get; set; }

        [Column("Tcno")]
        [MaxLength(11)]
        public string? Tcno { get; set; }

        [Column("Location")]  // Veritabanında zaten var
        [MaxLength(200)]
        public string? Location { get; set; }

        [Column("RoleId")]
        public int RoleId { get; set; } = 2;

        [Column("IsActive")]
        [Required]
        public bool IsActive { get; set; } = true;


        [Column("CreatedDate")]
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Column("UpdatedDate")]
        public DateTime? UpdatedDate { get; set; }
    }
}