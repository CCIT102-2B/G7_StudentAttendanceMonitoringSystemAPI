
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using StudentAttendanceMonitoringSystem;
using StudentAttendanceMonitoringSystem.API.Models;

namespace StudentAttendanceMonitoringSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "User")]
    public class StudentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(
            ApplicationDbContext context,
            ILogger<StudentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/students
        [HttpGet]
        public async Task<IActionResult> GetStudents()
        {
            var students = await _context.Students
                .Where(s => s.IsActive)
                .Select(s => new
                {
                    s.Id,
                    s.StudentId,
                    s.FirstName,
                    s.LastName,
                    s.Email,
                    s.Course,
                    s.YearLevel,
                    s.Section,
                    s.IsActive,
                    s.CreatedAt
                })
                .ToListAsync();

            return Ok(students);
        }

        // GET: api/students/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetStudent(int id)
        {
            var student = await _context.Students
                .Where(s => s.Id == id && s.IsActive)
                .Select(s => new
                {
                    s.Id,
                    s.StudentId,
                    s.FirstName,
                    s.LastName,
                    s.Email,
                    s.Course,
                    s.YearLevel,
                    s.Section,
                    s.IsActive,
                    s.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (student == null)
            {
                return NotFound(new
                {
                    message = "Student not found."
                });
            }

            return Ok(student);
        }

        // POST: api/students
        [HttpPost]
        public async Task<IActionResult> AddStudent(
            [FromBody] CreateStudentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.StudentId) ||
                string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName))
            {
                return BadRequest(new
                {
                    message = "Student ID, first name, and last name are required."
                });
            }

            string studentId = request.StudentId.Trim();

            bool exists;

            try
            {
                exists = await _context.Students.AnyAsync(s =>
                    s.StudentId == studentId);
            }
            catch (SqlException exception)
            {
                _logger.LogError(
                    exception,
                    "Database failed while checking StudentId {StudentId}.",
                    studentId
                );

                return StatusCode(500, new
                {
                    message = "The database could not check this student."
                });
            }

            if (exists)
            {
                return Conflict(new
                {
                    message = "Student ID already exists."
                });
            }

            var student = new Student
            {
                StudentId = studentId,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = request.Email?.Trim(),
                Course = request.Course,
                YearLevel = request.YearLevel,
                Section = request.Section,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Students.Add(student);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException exception)
            {
                var sqlException =
                    exception.GetBaseException() as SqlException;

                if (sqlException?.Number is 2601 or 2627)
                {
                    return Conflict(new
                    {
                        message = "Student ID already exists."
                    });
                }

                _logger.LogError(
                    exception,
                    "Database failed to create student {StudentId}.",
                    student.StudentId
                );

                return StatusCode(500, new
                {
                    message = "The database could not save this student."
                });
            }

            return Ok(new
            {
                message = "Student created successfully.",
                student = new
                {
                    student.Id,
                    student.StudentId,
                    student.FirstName,
                    student.LastName,
                    student.Email,
                    student.Course,
                    student.YearLevel,
                    student.Section,
                    student.IsActive,
                    student.CreatedAt
                }
            });
        }

        // PUT: api/students/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateStudent(
            int id,
            [FromBody] UpdateStudentRequest updatedStudent)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

            if (student == null)
            {
                return NotFound(new
                {
                    message = "Student not found."
                });
            }

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
            {
                return Conflict(new
                {
                    message = "Student ID already exists."
                });
            }

            student.StudentId = newStudentId;
            student.FirstName = updatedStudent.FirstName.Trim();
            student.LastName = updatedStudent.LastName.Trim();
            student.Email = updatedStudent.Email?.Trim();
            student.Course = updatedStudent.Course;
            student.YearLevel = updatedStudent.YearLevel;
            student.Section = updatedStudent.Section;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Student updated successfully.",
                student = new
                {
                    student.Id,
                    student.StudentId,
                    student.FirstName,
                    student.LastName,
                    student.Email,
                    student.Course,
                    student.YearLevel,
                    student.Section,
                    student.IsActive,
                    student.CreatedAt
                }
            });
        }

        // DELETE: api/students/1
        // SOFT DELETE - preserves attendance history
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

            if (student == null)
            {
                return NotFound(new
                {
                    message = "Student not found."
                });
            }

            student.IsActive = false;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Student successfully deactivated."
            });
        }
    }

    // ==========================================
    // REQUEST MODELS
    // ==========================================

    public class CreateStudentRequest
    {
        [Required]
        [StringLength(30)]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? Course { get; set; }

        [StringLength(20)]
        public string? YearLevel { get; set; }

        [StringLength(50)]
        public string? Section { get; set; }
    }

    public class UpdateStudentRequest
    {
        [Required]
        [StringLength(30)]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? Course { get; set; }

        [StringLength(20)]
        public string? YearLevel { get; set; }

        [StringLength(50)]
        public string? Section { get; set; }
    }
}
