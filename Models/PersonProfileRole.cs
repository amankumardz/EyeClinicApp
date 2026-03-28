namespace EyeClinicApp.Models
{
    public static class PersonProfileRole
    {
        public const string Doctor = "Doctor";
        public const string Optometrist = "Optometrist";
        public const string Staff = "Staff";

        public static readonly IReadOnlyCollection<string> AllowedRoles = new[] { Doctor, Optometrist, Staff };
    }
}
