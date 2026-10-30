
using Microsoft.EntityFrameworkCore;
using StudentAttendanceMonitoringSystem.API.Models;
using StudentAttendanceMonitoringSystem.Models;

namespace StudentAttendanceMonitoringSystem
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Attendance> AttendanceRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // STUDENTS TABLE
            modelBuilder.Entity<Student>(entity =>
            {
                entity.ToTable("Students");

                entity.HasKey(s => s.Id);

                entity.Property(s => s.Id)
                    .HasColumnName("StudentId");

                entity.Property(s => s.StudentId)
                    .HasColumnName("StudentNumber");

                entity.HasIndex(s => s.StudentId)
                    .IsUnique();
            });

            // ADMIN USERS TABLE
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("AdminUsers");

                entity.HasKey(u => u.Id);

                entity.Property(u => u.Id)
                    .HasColumnName("AdminId");

                entity.Ignore(u => u.Role);
            });

            // ATTENDANCE TABLE
            modelBuilder.Entity<Attendance>(entity =>
            {
                entity.ToTable("Attendance");

                entity.HasKey(a => a.Id);

                entity.Property(a => a.Id)
                    .HasColumnName("AttendanceId");

                entity.HasIndex(a => new
                {
                    a.StudentId,
                    a.AttendanceDate
                }).IsUnique();
            });
        }
    }
}
