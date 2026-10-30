
namespace StudentAttendanceMonitoringSystem.Models
{
    public class Attendance
    {
        public int Id { get; set; }

        public int StudentId { get; set; }

        public DateTime AttendanceDate { get; set; }

        public TimeSpan? TimeIn { get; set; }

        public TimeSpan? TimeOut { get; set; }

        public string Status { get; set; } = "Present";

        public string? Remarks { get; set; }
    }
}
