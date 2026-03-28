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
            await EnsureSchemaCompatibilityAsync(context);

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

            if (!await context.TimeSlots.AnyAsync())
            {
                await context.TimeSlots.AddRangeAsync(new[]
                {
                    new TimeSlot { StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(9, 30, 0), Label = "09:00 AM - 09:30 AM" },
                    new TimeSlot { StartTime = new TimeSpan(9, 30, 0), EndTime = new TimeSpan(10, 0, 0), Label = "09:30 AM - 10:00 AM" },
                    new TimeSlot { StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(10, 30, 0), Label = "10:00 AM - 10:30 AM" },
                    new TimeSlot { StartTime = new TimeSpan(10, 30, 0), EndTime = new TimeSpan(11, 0, 0), Label = "10:30 AM - 11:00 AM" },
                    new TimeSlot { StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(11, 30, 0), Label = "11:00 AM - 11:30 AM" },
                    new TimeSlot { StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(14, 30, 0), Label = "02:00 PM - 02:30 PM" },
                    new TimeSlot { StartTime = new TimeSpan(14, 30, 0), EndTime = new TimeSpan(15, 0, 0), Label = "02:30 PM - 03:00 PM" }
                });
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

            await context.SaveChangesAsync();

            if (!await context.Appointments.AnyAsync())
            {
                var firstSlotId = await context.TimeSlots.OrderBy(t => t.StartTime).Select(t => t.Id).FirstAsync();

                await context.Appointments.AddRangeAsync(new[]
                {
                    new Appointment
                    {
                        Name = "System Administrator",
                        PhoneNumber = "+1-555-123-4567",
                        NormalizedPhoneNumber = "15551234567",
                        Email = adminUser.Email,
                        AppointmentDate = DateTime.UtcNow.Date.AddDays(2),
                        TimeSlotId = firstSlotId,
                        Status = AppointmentStatus.Pending,
                        CreatedAtUtc = DateTime.UtcNow
                    },
                    new Appointment
                    {
                        Name = "John Doe",
                        PhoneNumber = "+1-555-777-0099",
                        NormalizedPhoneNumber = "15557770099",
                        Email = "john@example.com",
                        AppointmentDate = DateTime.UtcNow.Date.AddDays(-2),
                        TimeSlotId = firstSlotId,
                        Status = AppointmentStatus.Completed,
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-10),
                        UpdatedAtUtc = DateTime.UtcNow.AddDays(-2)
                    }
                });
            }

            await context.SaveChangesAsync();
        }

        public static string GetAdminCredentialsSummary() =>
            $"Admin login => Email: {AdminEmail}, Password: {AdminPassword}";

        private static async Task EnsureSchemaCompatibilityAsync(ApplicationDbContext context)
        {
            // Safety net for environments where migration history is out-of-sync with actual objects.
            // This keeps startup resilient on partially upgraded databases.
            await context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[TimeSlots]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TimeSlots](
        [Id] INT IDENTITY(1,1) NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_TimeSlots_IsActive] DEFAULT(1),
        [Label] nvarchar(100) NULL,
        CONSTRAINT [PK_TimeSlots] PRIMARY KEY ([Id])
    );
END

IF COL_LENGTH('dbo.Appointments', 'TimeSlotId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Appointments] ADD [TimeSlotId] INT NOT NULL CONSTRAINT [DF_Appointments_TimeSlotId] DEFAULT(1);
END

IF COL_LENGTH('dbo.Appointments', 'Name') IS NULL
BEGIN
    ALTER TABLE [dbo].[Appointments] ADD [Name] nvarchar(150) NOT NULL CONSTRAINT [DF_Appointments_Name] DEFAULT('Walk-in Client');
END

IF COL_LENGTH('dbo.Appointments', 'PhoneNumber') IS NULL
BEGIN
    ALTER TABLE [dbo].[Appointments] ADD [PhoneNumber] nvarchar(25) NOT NULL CONSTRAINT [DF_Appointments_PhoneNumber] DEFAULT('0000000000');
END

IF COL_LENGTH('dbo.Appointments', 'NormalizedPhoneNumber') IS NULL
BEGIN
    ALTER TABLE [dbo].[Appointments] ADD [NormalizedPhoneNumber] nvarchar(25) NOT NULL CONSTRAINT [DF_Appointments_NormalizedPhoneNumber] DEFAULT('0000000000');
END

IF COL_LENGTH('dbo.Appointments', 'Email') IS NULL
BEGIN
    ALTER TABLE [dbo].[Appointments] ADD [Email] nvarchar(150) NULL;
END

IF COL_LENGTH('dbo.Appointments', 'Age') IS NULL
BEGIN
    ALTER TABLE [dbo].[Appointments] ADD [Age] int NULL;
END

IF COL_LENGTH('dbo.Appointments', 'ReasonForVisit') IS NULL
BEGIN
    ALTER TABLE [dbo].[Appointments] ADD [ReasonForVisit] nvarchar(1000) NULL;
END

IF COL_LENGTH('dbo.Appointments', 'Address') IS NULL
BEGIN
    ALTER TABLE [dbo].[Appointments] ADD [Address] nvarchar(500) NULL;
END

IF COL_LENGTH('dbo.Appointments', 'ModifiedByAdminId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Appointments] ADD [ModifiedByAdminId] nvarchar(450) NULL;
END

IF COL_LENGTH('dbo.Appointments', 'CreatedAtUtc') IS NULL
BEGIN
    ALTER TABLE [dbo].[Appointments] ADD [CreatedAtUtc] datetime2 NOT NULL CONSTRAINT [DF_Appointments_CreatedAtUtc] DEFAULT(GETUTCDATE());
END

IF COL_LENGTH('dbo.Appointments', 'UpdatedAtUtc') IS NULL
BEGIN
    ALTER TABLE [dbo].[Appointments] ADD [UpdatedAtUtc] datetime2 NULL;
END

IF COL_LENGTH('dbo.Appointments', 'RowVersion') IS NULL
BEGIN
    ALTER TABLE [dbo].[Appointments] ADD [RowVersion] rowversion NULL;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Appointments_TimeSlots_TimeSlotId'
)
BEGIN
    ALTER TABLE [dbo].[Appointments] WITH NOCHECK
    ADD CONSTRAINT [FK_Appointments_TimeSlots_TimeSlotId]
        FOREIGN KEY([TimeSlotId]) REFERENCES [dbo].[TimeSlots]([Id]);
END

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Appointments_AspNetUsers_ModifiedByAdminId'
)
BEGIN
    ALTER TABLE [dbo].[Appointments] WITH NOCHECK
    ADD CONSTRAINT [FK_Appointments_AspNetUsers_ModifiedByAdminId]
        FOREIGN KEY([ModifiedByAdminId]) REFERENCES [dbo].[AspNetUsers]([Id]);
END
");
        }
    }
}
