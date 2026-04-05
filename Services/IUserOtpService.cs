using EyeClinicApp.Models;

namespace EyeClinicApp.Services
{
    public interface IUserOtpService
    {
        Task GenerateAndSendOtpAsync(ApplicationUser user, string purpose);
        Task<bool> VerifyOtpAsync(string userId, string purpose, string code);
    }
}
