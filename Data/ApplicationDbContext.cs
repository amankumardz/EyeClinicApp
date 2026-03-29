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
        }
    }
}
