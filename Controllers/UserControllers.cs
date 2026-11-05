
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using StudentAttendanceMonitoringSystem;
using StudentAttendanceMonitoringSystem.API.Models;

namespace StudentAttendanceMonitoringSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _userPasswordHasher;
        private readonly IConfiguration _configuration;

        public UsersController(
            ApplicationDbContext context,
            IPasswordHasher<User> userPasswordHasher,
            IConfiguration configuration)
        {
            _context = context;
            _userPasswordHasher = userPasswordHasher;
            _configuration = configuration;
        }

        // ==========================================
        // GET ALL USERS - ADMIN ONLY
        // GET: api/Users
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .AsNoTracking()
                .Select(user => new
                {
                    user.Id,
                    user.Email,
                    user.FullName,
                    user.Role
                })
                .ToListAsync();

            return Ok(users);
        }

        // ==========================================
        // UPDATE USER ROLE - ADMIN ONLY
        // PUT: api/Users/{id}/role
        // ==========================================
        [HttpPut("{id:int}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserRole(
            int id,
            [FromBody] UpdateUserRoleRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Role))
            {
                return BadRequest(new
                {
                    message = "A role is required."
                });
            }

            string role = request.Role.Trim();

            if (!role.Equals(
                    "Admin",
                    StringComparison.OrdinalIgnoreCase) &&
                !role.Equals(
                    "User",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "Role must be Admin or User."
                });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User not found."
                });
            }

            string normalizedRole = role.Equals(
                "Admin",
                StringComparison.OrdinalIgnoreCase)
                ? "Admin"
                : "User";

            // Prevent an administrator from removing
            // their own administrator access.
            string? currentUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (currentUserId == user.Id.ToString() &&
                normalizedRole != "Admin")
            {
                return BadRequest(new
                {
                    message = "You cannot remove your own Admin role."
                });
            }

            user.Role = normalizedRole;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User role updated successfully.",
                user = new
                {
                    user.Id,
                    user.Email,
                    user.FullName,
                    user.Role
                }
            });
        }

        // ==========================================
        // REGISTER USER
        // POST: api/Users/register
        // ==========================================
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                request.Password.Length < 8 ||
                string.IsNullOrWhiteSpace(request.FullName))
            {
                return BadRequest(new
                {
                    message = "Full name, email, and a password of at least 8 characters are required."
                });
            }

            string email = request.Email.Trim();

            bool exists = await _context.Users.AnyAsync(u =>
                u.Email.ToLower() == email.ToLower());

            if (exists)
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
                Role = "Pending"
            };

            user.PasswordHash = _userPasswordHasher.HashPassword(
                user,
                request.Password
            );

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User account created successfully. Account is pending administrator approval.",
                user = new
                {
                    user.Id,
                    user.Email,
                    user.FullName,
                    user.Role
                }
            });
        }

        // ==========================================
        // LOGIN USER
        // POST: api/Users/login
        // ==========================================
        [HttpPost("login")]
        [AllowAnonymous]
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

            if (!request.AccountType.Equals(
                    "User",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new
                {
                    message = "Invalid account type."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new
                {
                    message = "Email is required."
                });
            }

            string email = request.Email.Trim();

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email.ToLower() == email.ToLower());

            if (user == null ||
                string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            var result = _userPasswordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password
            );

            if (result == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            if (user.Role == "Pending")
            {
                return Unauthorized(new
                {
                    message = "Your account is pending activation. Ask the system administrator to assign your role."
                });
            }

            if (user.Role != "Admin" &&
                user.Role != "User")
            {
                return Unauthorized(new
                {
                    message = "Your account does not have a valid role."
                });
            }

            if (result ==
                PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash =
                    _userPasswordHasher.HashPassword(
                        user,
                        request.Password
                    );

                await _context.SaveChangesAsync();
            }

            DateTime issuedAt = DateTime.UtcNow;
            DateTime expiresAt = issuedAt.AddMinutes(60);

            string signingKey =
                _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "JWT signing key is not configured."
                );

            string issuer =
                _configuration["Jwt:Issuer"]
                ?? "AttendTrack";

            string audience =
                _configuration["Jwt:Audience"]
                ?? "AttendTrackFrontend";

            var claims = new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()
                ),
                new Claim(
                    ClaimTypes.Name,
                    user.FullName
                ),
                new Claim(
                    ClaimTypes.Email,
                    user.Email
                ),
                new Claim(
                    ClaimTypes.Role,
                    user.Role
                )
            };

            var signingCredentials = new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(signingKey)
                ),
                SecurityAlgorithms.HmacSha256
            );

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: issuedAt,
                expires: expiresAt,
                signingCredentials: signingCredentials
            );

            return Ok(new
            {
                message = "User login successful.",
                role = user.Role,
                accessToken =
                    new JwtSecurityTokenHandler()
                        .WriteToken(token),
                expiresAt,
                user = new
                {
                    user.Id,
                    user.Email,
                    user.FullName,
                    role = user.Role
                }
            });
        }
    }

    // ==========================================
    // LOGIN REQUEST MODEL
    // ==========================================
    public class LoginRequest
    {
        public string AccountType { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;

        public string Password { get; set; } =
            string.Empty;
    }

    // ==========================================
    // REGISTER REQUEST MODEL
    // ==========================================
    public class RegisterRequest
    {
        public string Email { get; set; } =
            string.Empty;

        public string Password { get; set; } =
            string.Empty;

        public string FullName { get; set; } =
            string.Empty;
    }

    // ==========================================
    // UPDATE ROLE REQUEST MODEL
    // ==========================================
    public class UpdateUserRoleRequest
    {
        public string Role { get; set; } =
            string.Empty;
    }
}
