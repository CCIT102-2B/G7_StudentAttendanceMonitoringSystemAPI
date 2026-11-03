
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
                    .HasColumnName("StudentNumber")
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(s => s.FirstName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(s => s.LastName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(s => s.Email)
                    .HasMaxLength(150)
                    .IsRequired(false);

                entity.Property(s => s.Course)
                    .HasMaxLength(100)
                    .IsRequired(false);

                entity.Property(s => s.YearLevel)
                    .HasMaxLength(20)
                    .IsRequired(false);

                entity.Property(s => s.Section)
                    .HasMaxLength(50)
                    .IsRequired(false);

                entity.Property(s => s.IsActive)
                    .IsRequired();

                entity.Property(s => s.CreatedAt)
                    .IsRequired();

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

                entity.Property(u => u.Email)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(u => u.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(u => u.FullName)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(u => u.CreatedAt)
                    .IsRequired();

                entity.Ignore(u => u.Role);

                entity.HasIndex(u => u.Email)
                    .IsUnique();
            });

            // ATTENDANCE TABLE
            modelBuilder.Entity<Attendance>(entity =>
            {
                entity.ToTable("Attendance");

                entity.HasKey(a => a.Id);

                entity.Property(a => a.Id)
                    .HasColumnName("AttendanceId");

                entity.Property(a => a.AttendanceDate)
                    .HasColumnType("date")
                    .IsRequired();

                entity.Property(a => a.TimeIn)
                    .HasColumnType("time");

                entity.Property(a => a.TimeOut)
                    .HasColumnType("time");

                entity.Property(a => a.Status)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(a => a.Remarks)
                    .HasMaxLength(500)
                    .IsRequired(false);

                entity.HasIndex(a => new
                {
                    a.StudentId,
                    a.AttendanceDate
                }).IsUnique();
            });
        }
    }
}
