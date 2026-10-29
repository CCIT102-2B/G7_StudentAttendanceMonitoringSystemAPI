using Microsoft.AspNetCore.Mvc;
using StudentAttendanceMonitoringSystem.API.Models;

namespace StudentAttendanceMonitoringSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private static readonly List<User> users = new();

        // GET: api/users
        [HttpGet]
        public IActionResult GetUsers()
        {
            var result = users.Select(user => new
            {
                user.Id,
                user.Username,
                user.Role
            });

            return Ok(result);
        }
    }
}