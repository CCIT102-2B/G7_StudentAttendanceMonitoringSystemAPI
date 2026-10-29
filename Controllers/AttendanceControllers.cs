using Microsoft.AspNetCore.Mvc;
using StudentAttendanceMonitoringSystem.API.Models;

namespace StudentAttendanceMonitoringSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceController : ControllerBase
    {
        private static readonly List<Attendance> records = new();
        private static int nextId = 1;

        // GET: api/attendance
        [HttpGet]
        public IActionResult GetAttendance()
        {
            return Ok(records);
        }

        // GET: api/attendance/1
        [HttpGet("{id:int}")]
        public IActionResult GetAttendanceById(int id)
        {
            var record = records.FirstOrDefault(a => a.Id == id);

            if (record == null)
                return NotFound(new { message = "Attendance record not found." });

            return Ok(record);
        }

        // POST: api/attendance
        [HttpPost]
        public IActionResult AddAttendance(Attendance attendance)
        {
            if (attendance.StudentId <= 0)
                return BadRequest(new { message = "Invalid Student ID." });

            if (attendance.Status is not ("Present" or "Late" or "Absent"))
                return BadRequest(new { message = "Invalid attendance status." });

            if (records.Any(a =>
                a.StudentId == attendance.StudentId &&
                a.AttendanceDate.Date == attendance.AttendanceDate.Date))
            {
                return Conflict(new
                {
                    message = "Attendance already exists for this student on this date."
                });
            }

            attendance.Id = nextId++;
            records.Add(attendance);

            return Ok(attendance);
        }

        // PUT: api/attendance/1
        [HttpPut("{id:int}")]
        public IActionResult UpdateAttendance(int id, Attendance updated)
        {
            var record = records.FirstOrDefault(a => a.Id == id);

            if (record == null)
                return NotFound(new { message = "Attendance record not found." });

            if (updated.StudentId <= 0)
                return BadRequest(new { message = "Invalid Student ID." });

            if (updated.Status is not ("Present" or "Late" or "Absent"))
                return BadRequest(new { message = "Invalid attendance status." });

            if (records.Any(a =>
                a.Id != id &&
                a.StudentId == updated.StudentId &&
                a.AttendanceDate.Date == updated.AttendanceDate.Date))
            {
                return Conflict(new
                {
                    message = "Attendance already exists for this student on this date."
                });
            }

            record.StudentId = updated.StudentId;
            record.AttendanceDate = updated.AttendanceDate;
            record.TimeIn = updated.TimeIn;
            record.TimeOut = updated.TimeOut;
            record.Status = updated.Status;

            return Ok(record);
        }

        // DELETE: api/attendance/1
        [HttpDelete("{id:int}")]
        public IActionResult DeleteAttendance(int id)
        {
            var record = records.FirstOrDefault(a => a.Id == id);

            if (record == null)
                return NotFound(new { message = "Attendance record not found." });

            records.Remove(record);

            return NoContent();
        }
    }
}