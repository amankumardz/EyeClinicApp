using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EyeClinicApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(150)]
        public string? FullName { get; set; }
    }
}
