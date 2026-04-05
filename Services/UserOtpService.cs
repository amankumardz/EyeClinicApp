using EyeClinicApp.Data;
using EyeClinicApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace EyeClinicApp.Services
{
    public class UserOtpService : IUserOtpService
    {
        public const string PurposeLogin = "login";
        public const string PurposeRegistration = "registration";

        private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<UserOtpService> _logger;

        public UserOtpService(ApplicationDbContext context, IEmailService emailService, ILogger<UserOtpService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task GenerateAndSendOtpAsync(ApplicationUser user, string purpose)
        {
            var now = DateTime.UtcNow;
            var code = GenerateSixDigitCode();

            var staleEntries = await _context.UserOtps
                .Where(o => o.UserId == user.Id && o.Purpose == purpose && (o.UsedAtUtc != null || o.ExpiryTime <= now))
                .ToListAsync();

            if (staleEntries.Count > 0)
            {
                _context.UserOtps.RemoveRange(staleEntries);
            }

            var activeEntries = await _context.UserOtps
                .Where(o => o.UserId == user.Id && o.Purpose == purpose && o.UsedAtUtc == null && o.ExpiryTime > now)
                .ToListAsync();

            if (activeEntries.Count > 0)
            {
                _context.UserOtps.RemoveRange(activeEntries);
            }

            var otp = new UserOtp
            {
                UserId = user.Id,
                Code = code,
                Purpose = purpose,
                ExpiryTime = now.Add(OtpLifetime)
            };

            _context.UserOtps.Add(otp);
            await _context.SaveChangesAsync();

            var recipient = user.Email?.Trim();
            if (string.IsNullOrWhiteSpace(recipient))
            {
                _logger.LogWarning("OTP generated but no email exists for UserId={UserId}.", user.Id);
                return;
            }

            var subject = purpose == PurposeRegistration
                ? "Verify your account - OTP code"
                : "Login verification code";
            var body = $"""
                        <p>Your one-time verification code is:</p>
                        <h2 style="letter-spacing:4px;">{code}</h2>
                        <p>This code expires in 5 minutes.</p>
                        <p>If you did not request this code, ignore this email.</p>
                        """;

            await _emailService.SendEmailAsync(recipient, subject, body);
        }

        public async Task<bool> VerifyOtpAsync(string userId, string purpose, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            var now = DateTime.UtcNow;
            var normalizedCode = new string(code.Where(char.IsDigit).ToArray());

            var otp = await _context.UserOtps
                .Where(o => o.UserId == userId && o.Purpose == purpose && o.UsedAtUtc == null && o.ExpiryTime > now)
                .OrderByDescending(o => o.CreatedAtUtc)
                .FirstOrDefaultAsync();

            if (otp is null || otp.Code != normalizedCode)
            {
                return false;
            }

            otp.UsedAtUtc = now;
            await _context.SaveChangesAsync();
            return true;
        }

        private static string GenerateSixDigitCode()
        {
            var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
            return value.ToString("D6");
        }
    }
}
