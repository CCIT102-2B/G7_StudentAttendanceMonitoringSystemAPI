namespace StudentAttendanceMonitoringSystem.API.Models
{
    public class Student
    {
        public int Id { get; set; }

        public string StudentId { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Course { get; set; } = string.Empty;

        public string YearLevel { get; set; } = string.Empty;
    }
}