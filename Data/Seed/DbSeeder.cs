using EyeClinicApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EyeClinicApp.Data.Seed
{
    public static class DbSeeder
    {
        private static readonly string[] RequiredRoles = [AppRoles.Admin, AppRoles.Doctor, AppRoles.Staff];
        private const string DefaultDoctorEmail = "doctor@eyeclinic.com";
        private const string DefaultDoctorPassword = "Doctor@123";
        private const string DefaultStaffEmail = "staff@eyeclinic.com";
        private const string DefaultStaffPassword = "Staff@123";

        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

            await context.Database.MigrateAsync();
            await EnsureSchemaCompatibilityAsync(context);

            foreach (var role in RequiredRoles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var adminEmail = configuration["AdminBootstrap:Email"]?.Trim();
            var adminPassword = configuration["AdminBootstrap:Password"];
            await EnsureBootstrapAdminAsync(userManager, logger, adminEmail, adminPassword);

            var doctorUser = await EnsureUserInRoleAsync(
                userManager,
                email: DefaultDoctorEmail,
                password: DefaultDoctorPassword,
                role: AppRoles.Doctor,
                fullName: "Dr. Default Physician");

            await EnsureUserInRoleAsync(
                userManager,
                email: DefaultStaffEmail,
                password: DefaultStaffPassword,
                role: AppRoles.Staff,
                fullName: "Default Front Desk Staff");

            var requiredSlots = GenerateStandardSlots().ToList();
            var existingSlots = await context.TimeSlots.ToListAsync();
            foreach (var required in requiredSlots)
            {
                var existing = existingSlots.FirstOrDefault(s => s.StartTime == required.StartTime && s.EndTime == required.EndTime);
                if (existing is null)
                {
                    await context.TimeSlots.AddAsync(required);
                    continue;
                }

                existing.Shift = required.Shift;
                existing.IsActive = true;
                existing.Label = required.Label;
            }

            if (!await context.Glasses.AnyAsync())
            {
                await context.Glasses.AddRangeAsync(new[]
                {
                    new Glass
                    {
                        Name = "Clarity Plus",
                        Brand = "VisionPro",
                        Category = "Men",
                        Price = 89.99m,
                        ImageBase64 = null,
                        Description = "Lightweight daily eyewear with anti-glare coating."
                    },
                    new Glass
                    {
                        Name = "Urban Focus",
                        Brand = "LensCraft",
                        Category = "Women",
                        Price = 129.50m,
                        ImageBase64 = null,
                        Description = "Modern acetate frame designed for all-day comfort."
                    },
                    new Glass
                    {
                        Name = "Reader Max",
                        Brand = "OptiLook",
                        Category = "Kids",
                        Price = 74.00m,
                        ImageBase64 = null,
                        Description = "Premium reading glasses with blue light filter."
                    }
                });
            }


            if (!await context.PersonProfiles.AnyAsync())
            {
                await context.PersonProfiles.AddRangeAsync(new[]
                {
                    new PersonProfile
                    {
                        Name = "Dr. Emma Carter",
                        Role = PersonProfileRole.Doctor,
                        Qualification = "MD Ophthalmology",
                        ExperienceYears = 12,
                        Achievements = "Gold Medal in Ophthalmic Surgery",
                        Bio = "Specialist in cataract and refractive care with patient-first consultation style.",
                        ProfileImageBase64 = null,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-30)
                    },
                    new PersonProfile
                    {
                        Name = "Dr. Noah Bennett",
                        Role = PersonProfileRole.Doctor,
                        Qualification = "MS Ophthalmology",
                        ExperienceYears = 9,
                        Achievements = "Published 15+ clinical papers",
                        Bio = "Focuses on glaucoma management and preventive eye health programs.",
                        ProfileImageBase64 = null,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-28)
                    },
                    new PersonProfile
                    {
                        Name = "Olivia Hayes",
                        Role = PersonProfileRole.Optometrist,
                        Qualification = "Doctor of Optometry",
                        ExperienceYears = 7,
                        Achievements = "Advanced contact lens fitting specialist",
                        Bio = "Supports vision testing, prescription accuracy, and modern lens guidance.",
                        ProfileImageBase64 = null,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-26)
                    },
                    new PersonProfile
                    {
                        Name = "Mia Brooks",
                        Role = PersonProfileRole.Staff,
                        Qualification = "Clinic Operations Certification",
                        ExperienceYears = 5,
                        Achievements = "Patient satisfaction champion",
                        Bio = "Coordinates front-desk workflows and ensures smooth appointment experiences.",
                        ProfileImageBase64 = null,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-24)
                    }
                });
            }

            if (!await context.Reviews.AnyAsync())
            {
                await context.Reviews.AddRangeAsync(new[]
                {
                    new Review
                    {
                        ClientName = "Sophia Turner",
                        Rating = 5,
                        ReviewText = "Excellent eye exam experience. The team was very professional and supportive.",
                        ClientImageBase64 = null,
                        IsApproved = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-10)
                    },
                    new Review
                    {
                        ClientName = "Liam Walker",
                        Rating = 5,
                        ReviewText = "Booking was simple and the doctor explained every detail clearly.",
                        ClientImageBase64 = null,
                        IsApproved = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-8)
                    },
                    new Review
                    {
                        ClientName = "Ava Thompson",
                        Rating = 4,
                        ReviewText = "Modern clinic, friendly staff, and quick turnaround for my prescription.",
                        ClientImageBase64 = null,
                        IsApproved = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-6)
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
                        Email = adminEmail,
                        AppointmentDate = DateTime.UtcNow.Date.AddDays(2),
                        TimeSlotId = firstSlotId,
                        Status = AppointmentStatus.Pending,
                        AssignedDoctorId = doctorUser.Id,
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
                        AssignedDoctorId = doctorUser.Id,
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-10),
                        UpdatedAtUtc = DateTime.UtcNow.AddDays(-2)
                    }
                });
            }

            var unassignedSampleAppointments = await context.Appointments
                .Where(a => a.AssignedDoctorId == null && (a.Email == adminEmail || a.Email == "john@example.com"))
                .ToListAsync();

            foreach (var appointment in unassignedSampleAppointments)
            {
                appointment.AssignedDoctorId = doctorUser.Id;
            }

            await context.SaveChangesAsync();
        }

        private static async Task<ApplicationUser?> EnsureBootstrapAdminAsync(
            UserManager<ApplicationUser> userManager,
            ILogger logger,
            string? adminEmail,
            string? adminPassword)
        {
            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                logger.LogWarning("Admin bootstrap skipped. Configure AdminBootstrap__Email and AdminBootstrap__Password environment variables for production.");
                return null;
            }

            if (!IsStrongBootstrapPassword(adminPassword))
            {
                logger.LogWarning("Admin bootstrap skipped because AdminBootstrap password is weak. It must be at least 12 chars with upper, lower, digit and symbol.");
                return null;
            }

            return await EnsureUserInRoleAsync(
                userManager,
                email: adminEmail,
                password: adminPassword,
                role: AppRoles.Admin,
                fullName: "System Administrator");
        }

        private static async Task<ApplicationUser> EnsureUserInRoleAsync(
            UserManager<ApplicationUser> userManager,
            string email,
            string password,
            string role,
            string fullName)
        {
            var normalizedEmail = email.Trim();
            var user = await userManager.FindByEmailAsync(normalizedEmail);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = normalizedEmail,
                    Email = normalizedEmail,
                    FullName = fullName,
                    EmailConfirmed = true,
                    LockoutEnabled = true
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create seeded user '{normalizedEmail}': {errors}");
                }
            }
            else
            {
                var shouldUpdate = false;
                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    shouldUpdate = true;
                }

                if (string.IsNullOrWhiteSpace(user.FullName))
                {
                    user.FullName = fullName;
                    shouldUpdate = true;
                }

                if (shouldUpdate)
                {
                    var updateResult = await userManager.UpdateAsync(user);
                    if (!updateResult.Succeeded)
                    {
                        var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Failed to update seeded user '{normalizedEmail}': {errors}");
                    }
                }
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                var roleResult = await userManager.AddToRoleAsync(user, role);
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to assign role '{role}' to '{normalizedEmail}': {errors}");
                }
            }

            return user;
        }

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
        [Shift] nvarchar(20) NOT NULL CONSTRAINT [DF_TimeSlots_Shift] DEFAULT('Morning'),
        [IsActive] bit NOT NULL CONSTRAINT [DF_TimeSlots_IsActive] DEFAULT(1),
        [Label] nvarchar(100) NULL,
        CONSTRAINT [PK_TimeSlots] PRIMARY KEY ([Id])
    );
END

IF COL_LENGTH('dbo.TimeSlots', 'Shift') IS NULL
BEGIN
    ALTER TABLE [dbo].[TimeSlots] ADD [Shift] nvarchar(20) NOT NULL CONSTRAINT [DF_TimeSlots_Shift] DEFAULT('Morning');
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

IF COL_LENGTH('dbo.Glasses', 'Category') IS NULL
BEGIN
    ALTER TABLE [dbo].[Glasses] ADD [Category] nvarchar(20) NOT NULL CONSTRAINT [DF_Glasses_Category] DEFAULT('Men');
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


IF OBJECT_ID(N'[dbo].[PersonProfiles]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PersonProfiles](
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Role] nvarchar(30) NOT NULL,
        [Qualification] nvarchar(200) NULL,
        [ExperienceYears] int NOT NULL,
        [Achievements] nvarchar(2000) NULL,
        [Bio] nvarchar(2000) NULL,
        [ProfileImageBase64] nvarchar(max) NULL,
        [IsActive] bit NOT NULL CONSTRAINT [DF_PersonProfiles_IsActive] DEFAULT(1),
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_PersonProfiles_CreatedAt] DEFAULT(GETUTCDATE()),
        CONSTRAINT [PK_PersonProfiles] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    IF COL_LENGTH('dbo.PersonProfiles', 'ProfileImageBase64') IS NULL
    BEGIN
        ALTER TABLE [dbo].[PersonProfiles] ADD [ProfileImageBase64] nvarchar(max) NULL;
    END
END

IF OBJECT_ID(N'[dbo].[Reviews]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Reviews](
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ClientName] nvarchar(150) NOT NULL,
        [Rating] int NOT NULL,
        [ReviewText] nvarchar(2000) NOT NULL,
        [ClientImageBase64] nvarchar(max) NULL,
        [IsApproved] bit NOT NULL CONSTRAINT [DF_Reviews_IsApproved] DEFAULT(0),
        [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_Reviews_CreatedAt] DEFAULT(GETUTCDATE()),
        CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    IF COL_LENGTH('dbo.Reviews', 'ClientImageBase64') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Reviews] ADD [ClientImageBase64] nvarchar(max) NULL;
    END
END

IF OBJECT_ID(N'[dbo].[Glasses]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Glasses', 'ImageBase64') IS NULL
    BEGIN
        ALTER TABLE [dbo].[Glasses] ADD [ImageBase64] nvarchar(max) NULL;
    END
END

IF OBJECT_ID(N'[dbo].[Orders]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Orders](
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] nvarchar(450) NULL,
        [Name] nvarchar(120) NOT NULL,
        [Phone] nvarchar(30) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [Address] nvarchar(500) NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id])
    );
END

IF COL_LENGTH('dbo.Orders', 'PaymentMethod') IS NULL
BEGIN
    ALTER TABLE [dbo].[Orders] ADD [PaymentMethod] nvarchar(30) NOT NULL CONSTRAINT [DF_Orders_PaymentMethod] DEFAULT('CashOnDelivery');
END

IF COL_LENGTH('dbo.Orders', 'PaymentStatus') IS NULL
BEGIN
    ALTER TABLE [dbo].[Orders] ADD [PaymentStatus] nvarchar(30) NOT NULL CONSTRAINT [DF_Orders_PaymentStatus] DEFAULT('Pending');
END

IF COL_LENGTH('dbo.Orders', 'PaymentId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Orders] ADD [PaymentId] nvarchar(200) NULL;
END

IF COL_LENGTH('dbo.Orders', 'RazorpayOrderId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Orders] ADD [RazorpayOrderId] nvarchar(200) NULL;
END

IF OBJECT_ID(N'[dbo].[OrderItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OrderItems](
        [Id] INT IDENTITY(1,1) NOT NULL,
        [OrderId] INT NOT NULL,
        [GlassId] INT NOT NULL,
        [Quantity] INT NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id])
    );
END

IF OBJECT_ID(N'[dbo].[CartItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CartItems](
        [Id] INT IDENTITY(1,1) NOT NULL,
        [GlassId] INT NOT NULL,
        [UserId] nvarchar(450) NULL,
        [Quantity] INT NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_CartItems] PRIMARY KEY ([Id])
    );
END

IF COL_LENGTH('dbo.CartItems', 'RightEyeSph') IS NULL
BEGIN
    ALTER TABLE [dbo].[CartItems] ADD [RightEyeSph] nvarchar(120) NULL;
END

IF COL_LENGTH('dbo.CartItems', 'RightEyeCyl') IS NULL
BEGIN
    ALTER TABLE [dbo].[CartItems] ADD [RightEyeCyl] nvarchar(120) NULL;
END

IF COL_LENGTH('dbo.CartItems', 'RightEyeAxis') IS NULL
BEGIN
    ALTER TABLE [dbo].[CartItems] ADD [RightEyeAxis] nvarchar(120) NULL;
END

IF COL_LENGTH('dbo.CartItems', 'LeftEyeSph') IS NULL
BEGIN
    ALTER TABLE [dbo].[CartItems] ADD [LeftEyeSph] nvarchar(120) NULL;
END

IF COL_LENGTH('dbo.CartItems', 'LeftEyeCyl') IS NULL
BEGIN
    ALTER TABLE [dbo].[CartItems] ADD [LeftEyeCyl] nvarchar(120) NULL;
END

IF COL_LENGTH('dbo.CartItems', 'LeftEyeAxis') IS NULL
BEGIN
    ALTER TABLE [dbo].[CartItems] ADD [LeftEyeAxis] nvarchar(120) NULL;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_UserId' AND object_id = OBJECT_ID(N'[dbo].[Orders]')
)
BEGIN
    CREATE INDEX [IX_Orders_UserId] ON [dbo].[Orders]([UserId]);
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderItems_OrderId' AND object_id = OBJECT_ID(N'[dbo].[OrderItems]')
)
BEGIN
    CREATE INDEX [IX_OrderItems_OrderId] ON [dbo].[OrderItems]([OrderId]);
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderItems_GlassId' AND object_id = OBJECT_ID(N'[dbo].[OrderItems]')
)
BEGIN
    CREATE INDEX [IX_OrderItems_GlassId] ON [dbo].[OrderItems]([GlassId]);
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = N'IX_CartItems_GlassId' AND object_id = OBJECT_ID(N'[dbo].[CartItems]')
)
BEGIN
    CREATE INDEX [IX_CartItems_GlassId] ON [dbo].[CartItems]([GlassId]);
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = N'IX_CartItems_UserId_GlassId' AND object_id = OBJECT_ID(N'[dbo].[CartItems]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_CartItems_UserId_GlassId] ON [dbo].[CartItems]([UserId], [GlassId]) WHERE [UserId] IS NOT NULL;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Orders_AspNetUsers_UserId'
)
BEGIN
    ALTER TABLE [dbo].[Orders] WITH NOCHECK
    ADD CONSTRAINT [FK_Orders_AspNetUsers_UserId]
        FOREIGN KEY([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]);
END

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_OrderItems_Orders_OrderId'
)
BEGIN
    ALTER TABLE [dbo].[OrderItems] WITH NOCHECK
    ADD CONSTRAINT [FK_OrderItems_Orders_OrderId]
        FOREIGN KEY([OrderId]) REFERENCES [dbo].[Orders]([Id]) ON DELETE CASCADE;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_OrderItems_Glasses_GlassId'
)
BEGIN
    ALTER TABLE [dbo].[OrderItems] WITH NOCHECK
    ADD CONSTRAINT [FK_OrderItems_Glasses_GlassId]
        FOREIGN KEY([GlassId]) REFERENCES [dbo].[Glasses]([Id]) ON DELETE NO ACTION;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CartItems_Glasses_GlassId'
)
BEGIN
    ALTER TABLE [dbo].[CartItems] WITH NOCHECK
    ADD CONSTRAINT [FK_CartItems_Glasses_GlassId]
        FOREIGN KEY([GlassId]) REFERENCES [dbo].[Glasses]([Id]) ON DELETE CASCADE;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CartItems_AspNetUsers_UserId'
)
BEGIN
    ALTER TABLE [dbo].[CartItems] WITH NOCHECK
    ADD CONSTRAINT [FK_CartItems_AspNetUsers_UserId]
        FOREIGN KEY([UserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE;
END
");
        }

        private static bool IsStrongBootstrapPassword(string password)
        {
            if (password.Length < 12)
            {
                return false;
            }

            return password.Any(char.IsUpper)
                && password.Any(char.IsLower)
                && password.Any(char.IsDigit)
                && password.Any(ch => !char.IsLetterOrDigit(ch));
        }

        private static IEnumerable<TimeSlot> GenerateStandardSlots()
        {
            var start = new TimeSpan(9, 0, 0);
            var end = new TimeSpan(20, 0, 0);
            var slotDuration = TimeSpan.FromMinutes(30);

            for (var current = start; current < end; current += slotDuration)
            {
                var next = current + slotDuration;
                yield return new TimeSlot
                {
                    StartTime = current,
                    EndTime = next,
                    Shift = TimeSlot.ResolveShift(current),
                    IsActive = true,
                    Label = $"{DateTime.Today.Add(current):hh:mm tt} - {DateTime.Today.Add(next):hh:mm tt}"
                };
            }
        }
    }
}
