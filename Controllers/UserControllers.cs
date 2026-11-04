
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
    [Authorize(Roles = "User")]
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

        // GET: api/users
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Select(user => new
                {
                    user.Id,
                    user.Email,
                    user.FullName,
                    Role = "User"
                })
                .ToListAsync();

            return Ok(users);
        }

        // POST: api/users/register
        [HttpPost("register")]
        [AllowAnonymous]
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
                Role = "User"
            };

            user.PasswordHash = _userPasswordHasher.HashPassword(
                user,
                request.Password
            );

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User account created successfully. Please log in.",
                user = new
                {
                    user.Id,
                    user.Email,
                    user.FullName,
                    role = "User"
                }
            });
        }

        // POST: api/users/login
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

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _userPasswordHasher.HashPassword(
                    user,
                    request.Password
                );

                await _context.SaveChangesAsync();
            }

            DateTime issuedAt = DateTime.UtcNow;
            DateTime expiresAt = issuedAt.AddMinutes(60);

            string signingKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "JWT signing key is not configured."
                );

            string issuer = _configuration["Jwt:Issuer"]
                ?? "AttendTrack";

            string audience = _configuration["Jwt:Audience"]
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
                    "User"
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
                role = "User",
                accessToken = new JwtSecurityTokenHandler()
                    .WriteToken(token),
                expiresAt,
                user = new
                {
                    user.Id,
                    user.Email,
                    user.FullName,
                    role = "User"
                }
            });
        }
    }

    public class LoginRequest
    {
        public string AccountType { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}
