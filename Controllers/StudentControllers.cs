
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentAttendanceMonitoringSystem;
using StudentAttendanceMonitoringSystem.API.Models;

namespace StudentAttendanceMonitoringSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StudentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/students
        [HttpGet]
        public async Task<IActionResult> GetStudents()
        {
            var students = await _context.Students.ToListAsync();
            return Ok(students);
        }

        // GET: api/students/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetStudent(int id)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return NotFound(new { message = "Student not found." });

            return Ok(student);
        }

        // POST: api/students
        [HttpPost]
        public async Task<IActionResult> AddStudent(Student student)
        {
            if (string.IsNullOrWhiteSpace(student.StudentId) ||
                string.IsNullOrWhiteSpace(student.FirstName) ||
                string.IsNullOrWhiteSpace(student.LastName))
            {
                return BadRequest(new
                {
                    message = "Student ID, first name, and last name are required."
                });
            }

            student.StudentId = student.StudentId.Trim();
            student.FirstName = student.FirstName.Trim();
            student.LastName = student.LastName.Trim();

            bool exists = await _context.Students.AnyAsync(s =>
                s.StudentId == student.StudentId);

            if (exists)
                return Conflict(new { message = "Student ID already exists." });

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return Ok(student);
        }

        // PUT: api/students/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateStudent(
            int id, Student updatedStudent)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return NotFound(new { message = "Student not found." });

            if (string.IsNullOrWhiteSpace(updatedStudent.StudentId) ||
                string.IsNullOrWhiteSpace(updatedStudent.FirstName) ||
                string.IsNullOrWhiteSpace(updatedStudent.LastName))
            {
                return BadRequest(new
                {
                    message = "Student ID, first name, and last name are required."
                });
            }

            string newStudentId = updatedStudent.StudentId.Trim();

            bool exists = await _context.Students.AnyAsync(s =>
                s.Id != id && s.StudentId == newStudentId);

            if (exists)
                return Conflict(new { message = "Student ID already exists." });

            student.StudentId = newStudentId;
            student.FirstName = updatedStudent.FirstName.Trim();
            student.LastName = updatedStudent.LastName.Trim();
            student.Course = updatedStudent.Course;
            student.YearLevel = updatedStudent.YearLevel;

            await _context.SaveChangesAsync();

            return Ok(student);
        }

        // DELETE: api/students/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return NotFound(new { message = "Student not found." });

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
