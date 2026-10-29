using Microsoft.AspNetCore.Mvc;
using StudentAttendanceMonitoringSystem.API.Models;

namespace StudentAttendanceMonitoringSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentsController : ControllerBase
    {
        private static readonly List<Student> students = new();
        private static int nextId = 1;

        // GET: api/students
        [HttpGet]
        public IActionResult GetStudents()
        {
            return Ok(students);
        }

        // GET: api/students/1
        [HttpGet("{id:int}")]
        public IActionResult GetStudent(int id)
        {
            var student = students.FirstOrDefault(s => s.Id == id);

            if (student == null)
                return NotFound(new { message = "Student not found." });

            return Ok(student);
        }

        // POST: api/students
        [HttpPost]
        public IActionResult AddStudent(Student student)
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

            if (students.Any(s =>
                s.StudentId.Equals(
                    student.StudentId.Trim(),
                    StringComparison.OrdinalIgnoreCase)))
            {
                return Conflict(new { message = "Student ID already exists." });
            }

            student.Id = nextId++;
            student.StudentId = student.StudentId.Trim();
            student.FirstName = student.FirstName.Trim();
            student.LastName = student.LastName.Trim();

            students.Add(student);

            return Ok(student);
        }

        // PUT: api/students/1
        [HttpPut("{id:int}")]
        public IActionResult UpdateStudent(int id, Student updatedStudent)
        {
            var student = students.FirstOrDefault(s => s.Id == id);

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

            if (students.Any(s =>
                s.Id != id &&
                s.StudentId.Equals(
                    updatedStudent.StudentId.Trim(),
                    StringComparison.OrdinalIgnoreCase)))
            {
                return Conflict(new { message = "Student ID already exists." });
            }

            student.StudentId = updatedStudent.StudentId.Trim();
            student.FirstName = updatedStudent.FirstName.Trim();
            student.LastName = updatedStudent.LastName.Trim();
            student.Course = updatedStudent.Course;
            student.YearLevel = updatedStudent.YearLevel;

            return Ok(student);
        }

        // DELETE: api/students/1
        [HttpDelete("{id:int}")]
        public IActionResult DeleteStudent(int id)
        {
            var student = students.FirstOrDefault(s => s.Id == id);

            if (student == null)
                return NotFound(new { message = "Student not found." });

            students.Remove(student);

            return NoContent();
        }
    }
}