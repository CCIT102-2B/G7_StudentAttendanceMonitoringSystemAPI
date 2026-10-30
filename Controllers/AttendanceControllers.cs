
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentAttendanceMonitoringSystem;
using StudentAttendanceMonitoringSystem.Models;

namespace StudentAttendanceMonitoringSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttendanceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/attendance
        [HttpGet]
        public async Task<IActionResult> GetAttendance()
        {
            var records = await _context.AttendanceRecords.ToListAsync();
            return Ok(records);
        }

        // GET: api/attendance/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetAttendanceById(int id)
        {
            var record = await _context.AttendanceRecords
                .FirstOrDefaultAsync(a => a.Id == id);

            if (record == null)
                return NotFound(new { message = "Attendance record not found." });

            return Ok(record);
        }

        // POST: api/attendance
        [HttpPost]
        public async Task<IActionResult> AddAttendance(Attendance attendance)
        {
            if (attendance.StudentId <= 0)
                return BadRequest(new { message = "Invalid Student ID." });

            if (attendance.Status is not ("Present" or "Late" or "Absent"))
                return BadRequest(new { message = "Invalid attendance status." });

            bool studentExists = await _context.Students
                .AnyAsync(s => s.Id == attendance.StudentId);

            if (!studentExists)
                return BadRequest(new { message = "Student does not exist." });

            bool alreadyExists = await _context.AttendanceRecords.AnyAsync(a =>
                a.StudentId == attendance.StudentId &&
                a.AttendanceDate.Date == attendance.AttendanceDate.Date);

            if (alreadyExists)
                return Conflict(new
                {
                    message = "Attendance already exists for this student on this date."
                });

            _context.AttendanceRecords.Add(attendance);
            await _context.SaveChangesAsync();

            return Ok(attendance);
        }

        // PUT: api/attendance/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateAttendance(
            int id, Attendance updated)
        {
            var record = await _context.AttendanceRecords
                .FirstOrDefaultAsync(a => a.Id == id);

            if (record == null)
                return NotFound(new { message = "Attendance record not found." });

            if (updated.StudentId <= 0)
                return BadRequest(new { message = "Invalid Student ID." });

            if (updated.Status is not ("Present" or "Late" or "Absent"))
                return BadRequest(new { message = "Invalid attendance status." });

            bool studentExists = await _context.Students
                .AnyAsync(s => s.Id == updated.StudentId);

            if (!studentExists)
                return BadRequest(new { message = "Student does not exist." });

            bool duplicate = await _context.AttendanceRecords.AnyAsync(a =>
                a.Id != id &&
                a.StudentId == updated.StudentId &&
                a.AttendanceDate.Date == updated.AttendanceDate.Date);

            if (duplicate)
                return Conflict(new
                {
                    message = "Attendance already exists for this student on this date."
                });

            record.StudentId = updated.StudentId;
            record.AttendanceDate = updated.AttendanceDate;
            record.TimeIn = updated.TimeIn;
            record.TimeOut = updated.TimeOut;
            record.Status = updated.Status;
            record.Remarks = updated.Remarks;

            await _context.SaveChangesAsync();

            return Ok(record);
        }

        // DELETE: api/attendance/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteAttendance(int id)
        {
            var record = await _context.AttendanceRecords
                .FirstOrDefaultAsync(a => a.Id == id);

            if (record == null)
                return NotFound(new { message = "Attendance record not found." });

            _context.AttendanceRecords.Remove(record);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
