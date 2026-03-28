using System.ComponentModel.DataAnnotations;

namespace EyeClinicApp.Models
{
    public class PersonProfile
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        [RegularExpression("Doctor|Optometrist|Staff", ErrorMessage = "Role must be Doctor, Optometrist, or Staff.")]
        public string Role { get; set; } = PersonProfileRole.Doctor;

        [MaxLength(200)]
        public string? Qualification { get; set; }

        [Range(0, 80)]
        public int ExperienceYears { get; set; }

        [MaxLength(2000)]
        public string? Achievements { get; set; }

        [MaxLength(2000)]
        public string? Bio { get; set; }

        public string? ProfileImageBase64 { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
