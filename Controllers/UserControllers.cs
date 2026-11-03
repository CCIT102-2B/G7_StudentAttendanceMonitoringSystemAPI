
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentAttendanceMonitoringSystem;
using StudentAttendanceMonitoringSystem.API.Models;

namespace StudentAttendanceMonitoringSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _userPasswordHasher;
        private readonly IPasswordHasher<Student> _studentPasswordHasher;

        public UsersController(
            ApplicationDbContext context,
            IPasswordHasher<User> userPasswordHasher,
            IPasswordHasher<Student> studentPasswordHasher)
        {
            _context = context;
            _userPasswordHasher = userPasswordHasher;
            _studentPasswordHasher = studentPasswordHasher;
        }

        // GET: api/users
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var result = await _context.Users
                .Select(user => new
                {
                    user.Id,
                    user.Email,
                    user.FullName,
                    user.Role
                })
                .ToListAsync();

            return Ok(result);
        }

        // POST: api/users/register
        // Teacher registration
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.FullName))
            {
                return BadRequest(new
                {
                    message = "Full name, email, and password are required."
                });
            }

            var email = request.Email.Trim();

            var existingUser = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email.ToLower());

            if (existingUser)
            {
                return Conflict(new
                {
                    message = "Email is already registered."
                });
            }

            var user = new User
            {
                Email = email,
                FullName = request.FullName.Trim(),
                CreatedAt = DateTime.UtcNow,
                Role = "Teacher"
            };

            user.PasswordHash = _userPasswordHasher.HashPassword(
                user,
                request.Password
            );

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Teacher account created successfully.",
                teacher = new
                {
                    user.Id,
                    user.Email,
                    user.FullName,
                    user.Role
                }
            });
        }

        // POST: api/users/login
        // Teacher and Student login
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.AccountType) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    message = "Account type and password are required."
                });
            }

            // TEACHER LOGIN
            if (request.AccountType.Equals(
                "Teacher",
                StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return BadRequest(new
                    {
                        message = "Teacher email is required."
                    });
                }

                var email = request.Email.Trim();

                var teacher = await _context.Users
                    .FirstOrDefaultAsync(u =>
                        u.Email.ToLower() == email.ToLower());

                if (teacher == null)
                {
                    return Unauthorized(new
                    {
                        message = "Invalid teacher email or password."
                    });
                }

                var result = _userPasswordHasher.VerifyHashedPassword(
                    teacher,
                    teacher.PasswordHash,
                    request.Password
                );

                if (result == PasswordVerificationResult.Failed)
                {
                    return Unauthorized(new
                    {
                        message = "Invalid teacher email or password."
                    });
                }

                if (result == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    teacher.PasswordHash =
                        _userPasswordHasher.HashPassword(
                            teacher,
                            request.Password
                        );

                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = "Teacher login successful.",
                    role = "Teacher",
                    teacher = new
                    {
                        teacher.Id,
                        teacher.Email,
                        teacher.FullName,
                        teacher.Role
                    }
                });
            }

            // STUDENT LOGIN
            if (request.AccountType.Equals(
                "Student",
                StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(request.StudentId))
                {
                    return BadRequest(new
                    {
                        message = "Student ID is required."
                    });
                }

                var studentId = request.StudentId.Trim();

                var student = await _context.Students
                    .FirstOrDefaultAsync(s =>
                        s.StudentId == studentId);

                if (student == null || !student.IsActive)
                {
                    return Unauthorized(new
                    {
                        message = "Invalid student ID or password."
                    });
                }

                if (string.IsNullOrWhiteSpace(student.PasswordHash))
                {
                    return Unauthorized(new
                    {
                        message = "Student account has no password yet. Please contact your teacher."
                    });
                }

                var result = _studentPasswordHasher.VerifyHashedPassword(
                    student,
                    student.PasswordHash,
                    request.Password
                );

                if (result == PasswordVerificationResult.Failed)
                {
                    return Unauthorized(new
                    {
                        message = "Invalid student ID or password."
                    });
                }

                if (result == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    student.PasswordHash =
                        _studentPasswordHasher.HashPassword(
                            student,
                            request.Password
                        );

                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = "Student login successful.",
                    role = "Student",
                    student = new
                    {
                        student.Id,
                        student.StudentId,
                        student.FirstName,
                        student.LastName,
                        student.Email,
                        student.Course,
                        student.YearLevel,
                        student.Section
                    }
                });
            }

            return BadRequest(new
            {
                message = "Invalid account type. Choose Teacher or Student."
            });
        }
    }

    // REQUEST MODELS

    public class LoginRequest
    {
        public string AccountType { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}
