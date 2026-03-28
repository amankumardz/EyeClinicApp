using EyeClinicApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EyeClinicApp.Data.Seed
{
    public static class DbSeeder
    {
        private const string AdminRoleName = "Admin";
        private const string AdminEmail = "admin@eyeclinic.local";
        private const string AdminPassword = "Admin@12345";

        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await context.Database.MigrateAsync();

            if (!await roleManager.RoleExistsAsync(AdminRoleName))
            {
                await roleManager.CreateAsync(new IdentityRole(AdminRoleName));
            }

            var adminUser = await userManager.FindByEmailAsync(AdminEmail);
            if (adminUser is null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = AdminEmail,
                    Email = AdminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(adminUser, AdminPassword);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create admin user: {errors}");
                }
            }

            if (!await userManager.IsInRoleAsync(adminUser, AdminRoleName))
            {
                await userManager.AddToRoleAsync(adminUser, AdminRoleName);
            }

            if (!await context.Glasses.AnyAsync())
            {
                await context.Glasses.AddRangeAsync(new[]
                {
                    new Glass
                    {
                        Name = "Clarity Plus",
                        Brand = "VisionPro",
                        Price = 89.99m,
                        ImageUrl = "https://images.unsplash.com/photo-1511497584788-876760111969?auto=format&fit=crop&w=1200&q=80",
                        Description = "Lightweight daily eyewear with anti-glare coating."
                    },
                    new Glass
                    {
                        Name = "Urban Focus",
                        Brand = "LensCraft",
                        Price = 129.50m,
                        ImageUrl = "https://images.unsplash.com/photo-1577803645773-f96470509666?auto=format&fit=crop&w=1200&q=80",
                        Description = "Modern acetate frame designed for all-day comfort."
                    },
                    new Glass
                    {
                        Name = "Reader Max",
                        Brand = "OptiLook",
                        Price = 74.00m,
                        ImageUrl = "https://images.unsplash.com/photo-1487412720507-e7ab37603c6f?auto=format&fit=crop&w=1200&q=80",
                        Description = "Premium reading glasses with blue light filter."
                    }
                });
            }

            if (!await context.Appointments.AnyAsync())
            {
                await context.Appointments.AddRangeAsync(new[]
                {
                    new Appointment
                    {
                        UserId = adminUser.Id,
                        AppointmentDate = DateTime.UtcNow.AddDays(2),
                        Status = "Pending"
                    },
                    new Appointment
                    {
                        UserId = adminUser.Id,
                        AppointmentDate = DateTime.UtcNow.AddDays(7),
                        Status = "Confirmed"
                    }
                });
            }

            await context.SaveChangesAsync();
        }

        public static string GetAdminCredentialsSummary() =>
            $"Admin login => Email: {AdminEmail}, Password: {AdminPassword}";
    }
}
