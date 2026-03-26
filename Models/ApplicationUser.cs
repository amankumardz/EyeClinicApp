using Microsoft.AspNetCore.Identity;

namespace EyeClinicApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
    }
}
