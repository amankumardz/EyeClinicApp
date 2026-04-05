#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EyeClinicApp.Models;
using EyeClinicApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace EyeClinicApp.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public class VerifyOtpModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUserOtpService _userOtpService;
        private readonly ILogger<VerifyOtpModel> _logger;

        public VerifyOtpModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IUserOtpService userOtpService,
            ILogger<VerifyOtpModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userOtpService = userOtpService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string UserId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Purpose { get; set; } = UserOtpService.PurposeLogin;

        [BindProperty(SupportsGet = true)]
        public bool RememberMe { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "OTP code")]
            [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be a 6-digit code.")]
            public string Code { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(UserId))
            {
                return RedirectToPage("./Login");
            }

            var user = await _userManager.FindByIdAsync(UserId);
            if (user is null)
            {
                return RedirectToPage("./Login");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ReturnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (string.IsNullOrWhiteSpace(UserId))
            {
                ModelState.AddModelError(string.Empty, "Session expired. Please login again.");
                return Page();
            }

            var user = await _userManager.FindByIdAsync(UserId);
            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Invalid verification request.");
                return Page();
            }

            var isValidOtp = await _userOtpService.VerifyOtpAsync(user.Id, Purpose, Input.Code);
            if (!isValidOtp)
            {
                ModelState.AddModelError(string.Empty, "Invalid or expired OTP.");
                return Page();
            }

            if (Purpose == UserOtpService.PurposeRegistration && !user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
                _logger.LogInformation("Email confirmed by OTP for UserId={UserId}.", user.Id);
            }

            if (Purpose == UserOtpService.PurposeLogin)
            {
                var fullName = string.IsNullOrWhiteSpace(user.FullName)
                    ? (user.Email ?? user.UserName ?? "User")
                    : user.FullName;

                await _signInManager.SignInWithClaimsAsync(user, RememberMe, new[]
                {
                    new Claim("FullName", fullName)
                });
            }

            return LocalRedirect(ReturnUrl);
        }
    }
}
