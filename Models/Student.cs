
namespace StudentAttendanceMonitoringSystem.API.Models
{
    public class Student
    {
        public int Id { get; set; }

        public string StudentId { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string Course { get; set; } = string.Empty;

        public string YearLevel { get; set; } = string.Empty;

        public string? Section { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
    }
}
