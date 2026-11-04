
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentAttendanceMonitoringSystem;
using StudentAttendanceMonitoringSystem.API.Models;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// JWT CONFIGURATION
// ==========================================

var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey) ||
    Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Configure Jwt:Key with a secret of at least 32 UTF-8 bytes.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AttendTrack";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AttendTrackFrontend";

// ==========================================
// SERVICES
// ==========================================

builder.Services.AddControllers();

builder.Services.AddOpenApi();

// ==========================================
// DATABASE CONNECTION
// ==========================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// ==========================================
// PASSWORD HASHERS
// ==========================================

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddScoped<IPasswordHasher<Student>, PasswordHasher<Student>>();

// ==========================================
// JWT AUTHENTICATION
// ==========================================

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),

            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidAlgorithms = new[]
            {
                SecurityAlgorithms.HmacSha256
            },

            ValidateLifetime = true,

            RoleClaimType = ClaimTypes.Role,

            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

// ==========================================
// AUTHORIZATION
// ==========================================

builder.Services.AddAuthorization();

// ==========================================
// CORS CONFIGURATION
// ==========================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ==========================================
// BUILD APPLICATION
// ==========================================

var app = builder.Build();

// ==========================================
// OPENAPI AND SCALAR
// ==========================================

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// ==========================================
// MIDDLEWARE
// ==========================================

app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseAuthorization();

// ==========================================
// CONTROLLERS
// ==========================================

app.MapControllers();

// ==========================================
// RUN APPLICATION
// ==========================================

app.Run();
