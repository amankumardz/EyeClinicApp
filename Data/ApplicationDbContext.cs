using EyeClinicApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EyeClinicApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Glass> Glasses { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }
        public DbSet<PersonProfile> PersonProfiles { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<UserOtp> UserOtps { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<TimeSlot>()
                .HasIndex(t => new { t.StartTime, t.EndTime })
                .IsUnique();

            builder.Entity<TimeSlot>()
                .Property(t => t.Shift)
                .HasMaxLength(20);

            builder.Entity<Appointment>()
                .HasIndex(a => new { a.AppointmentDate, a.TimeSlotId })
                .IsUnique()
                .HasFilter($"[{nameof(Appointment.Status)}] <> '{AppointmentStatus.Rejected}' AND [{nameof(Appointment.Status)}] <> '{AppointmentStatus.Completed}'");

            builder.Entity<Appointment>()
                .HasIndex(a => new { a.NormalizedPhoneNumber, a.Status });

            builder.Entity<Appointment>()
                .HasOne(a => a.TimeSlot)
                .WithMany(t => t.Appointments)
                .HasForeignKey(a => a.TimeSlotId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Appointment>()
                .HasOne(a => a.ModifiedByAdmin)
                .WithMany()
                .HasForeignKey(a => a.ModifiedByAdminId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Appointment>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Appointment>()
                .HasOne(a => a.AssignedDoctor)
                .WithMany()
                .HasForeignKey(a => a.AssignedDoctorId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Appointment>()
                .HasIndex(a => new { a.UserId, a.AppointmentDate });

            builder.Entity<Appointment>()
                .HasIndex(a => a.AssignedDoctorId);

            builder.Entity<Glass>()
                .Property(g => g.Price)
                .HasPrecision(18, 2);

            builder.Entity<Appointment>()
                .Property(a => a.AppointmentDate)
                .HasColumnType("date");

            builder.Entity<PersonProfile>()
                .Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Entity<Review>()
                .Property(r => r.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Entity<CartItem>()
                .HasIndex(c => new { c.UserId, c.GlassId })
                .IsUnique()
                .HasFilter("[UserId] IS NOT NULL");

            builder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(o => o.Price)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderItem>()
                .HasOne(i => i.Glass)
                .WithMany()
                .HasForeignKey(i => i.GlassId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CartItem>()
                .HasOne(c => c.Glass)
                .WithMany()
                .HasForeignKey(c => c.GlassId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CartItem>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Prescription>()
                .HasOne(p => p.Appointment)
                .WithOne(a => a.Prescription)
                .HasForeignKey<Prescription>(p => p.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Prescription>()
                .HasOne(p => p.Doctor)
                .WithMany()
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Prescription>()
                .HasIndex(p => p.AppointmentId)
                .IsUnique();

            builder.Entity<UserOtp>()
                .HasIndex(o => new { o.UserId, o.Purpose, o.ExpiryTime });

            builder.Entity<UserOtp>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
