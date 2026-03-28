using EyeClinicApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EyeClinicApp.Data
{
    public static class DbInitializer
    {
        private const string AdminRole = "Admin";
        private const string AdminEmail = "admin@eyeclinic.local";
        private const string AdminPassword = "Admin@12345";

        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();

            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            if (!await roleManager.RoleExistsAsync(AdminRole))
            {
                await roleManager.CreateAsync(new IdentityRole(AdminRole));
            }

            var adminUser = await userManager.FindByEmailAsync(AdminEmail);
            if (adminUser is null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = AdminEmail,
                    Email = AdminEmail,
                    EmailConfirmed = true,
                    FullName = "System Admin"
                };

                var createResult = await userManager.CreateAsync(adminUser, AdminPassword);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create admin user: {errors}");
                }
            }

            if (!await userManager.IsInRoleAsync(adminUser, AdminRole))
            {
                await userManager.AddToRoleAsync(adminUser, AdminRole);
            }

            if (!await context.Glasses.AnyAsync())
            {
                context.Glasses.AddRange(
                    new Glass
                    {
                        Name = "Classic Frame",
                        Brand = "Ray-Ban",
                        Price = 149.99m,
                        ImageUrl = "https://images.unsplash.com/photo-1511499767150-a48a237f0083",
                        Description = "Lightweight metal frame with anti-glare lenses."
                    },
                    new Glass
                    {
                        Name = "Blue Light Shield",
                        Brand = "Lenskart",
                        Price = 89.50m,
                        ImageUrl = "https://images.unsplash.com/photo-1574258495973-f010dfbb5371",
                        Description = "Blue-light filtering glasses for long work sessions."
                    },
                    new Glass
                    {
                        Name = "Sport Vision",
                        Brand = "Oakley",
                        Price = 199.00m,
                        ImageUrl = "https://images.unsplash.com/photo-1511920170033-f8396924c348",
                        Description = "Durable sports eyewear with polarized lenses."
                    }
                );
            }

            if (!await context.Appointments.AnyAsync())
            {
                context.Appointments.AddRange(
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
                );
            }

            await context.SaveChangesAsync();
        }
    }
}
