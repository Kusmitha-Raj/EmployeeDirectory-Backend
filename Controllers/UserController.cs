using EmployeeDirectoryApp.Data;
using EmployeeDirectoryApp.DTO;
using EmployeeDirectoryApp.Models.Entities;
using EmployeeDirectoryApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwtService;
    private readonly IWebHostEnvironment _env;

    public UserController(IWebHostEnvironment env,AppDbContext context, JwtService jwtService)
    {
        _env = env;
        _context = context; 
        _jwtService = jwtService;
    }
    [HttpPost("AddEmployee")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddEmployee([FromBody] CreateEmployeeDto dto)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        var existingEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == dto.Email);

        if (existingUser != null && existingEmployee != null)
            return BadRequest("This employee already exists.");

        User user;
        if (existingUser == null)
        {
            var defaultPassword = dto.FirstName + "@123";
            user = new User
            {
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
                Role = "Employee",
                MustChangePassword = true
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        else
        {
            user = existingUser;
        }

        if (existingEmployee == null)
        {
            var employee = new Employee
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Gender = dto.Gender,
                JobRole = dto.JobRole,
                DepartmentId = dto.DepartmentId,
                PhoneNo = dto.PhoneNo,
                UserId = user.UserId
            };
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
        }

        return Ok(new
        {
            Message = "Employee and user account created successfully",
            DefaultPassword = existingUser == null ? dto.FirstName + "@123" : "Already has a password"
        });
    }
    [HttpPost("AddAdmin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddAdmin([FromBody] CreateAdminDto dto)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existingUser != null)
            return BadRequest("Email already registered");

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = "Admin",
            MustChangePassword = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Admin created successfully", defaultPassword = "Admin@123" });
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return BadRequest("Invalid email or password");

        var accessToken = _jwtService.GenerateToken(user);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var refreshToken = _jwtService.GenerateRefreshToken(ipAddress ?? "unknown", user.UserId);

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = refreshToken.Expires,
            Secure = !_env.IsDevelopment(),
            SameSite = SameSiteMode.None
        };
        Response.Cookies.Append("refreshToken", refreshToken.Token, cookieOptions);

        return Ok(new { message = "Login successful", token = accessToken, role = user.Role });
    }
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        string token = request.RefreshToken ?? Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(token)) return Unauthorized();

        var stored = await _context.RefreshTokens.Include(rt => rt.User).FirstOrDefaultAsync(rt => rt.Token == token);
        if (stored == null || !stored.IsActive) return Unauthorized();

        stored.Revoked = DateTime.UtcNow;
        stored.RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        _context.RefreshTokens.Update(stored);

        var newRefreshToken = _jwtService.GenerateRefreshToken(HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown", stored.UserId);
        newRefreshToken.ReplacedByToken = null;
        _context.RefreshTokens.Add(newRefreshToken);

        
        var user = await _context.Users.FindAsync(stored.UserId);
        if (user == null) return Unauthorized();

        var newAccessToken = _jwtService.GenerateToken(user);

        await _context.SaveChangesAsync();


        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = newRefreshToken.Expires,
            Secure = !_env.IsDevelopment(),
            SameSite = SameSiteMode.None
        };
        Response.Cookies.Append("refreshToken", newRefreshToken.Token, cookieOptions);

        return Ok(new { token = newAccessToken });
    }
    //[HttpPost("ChangePassword")]
    //public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    //{
    //    if (string.IsNullOrWhiteSpace(dto.NewPassword))
    //        return BadRequest("New password is required");
    //    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
    //    if (user == null) return NotFound("User not found");

    //    if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
    //        return BadRequest("Old password is incorrect");
    //    if (dto.OldPassword == dto.NewPassword)
    //        return BadRequest("Please enter a different new password");
    //    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
    //    user.MustChangePassword = false;
    //    await _context.SaveChangesAsync();
    //    return Ok("Password updated successfully.Please log in again with your new password.");
    //}
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RefreshRequest req)
    {
        var token = req.RefreshToken ?? Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(token)) return BadRequest("Token required");

        var stored = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
        if (stored == null || stored.IsRevoked) return NotFound();

        stored.Revoked = DateTime.UtcNow;
        stored.RevokedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        _context.RefreshTokens.Update(stored);
        await _context.SaveChangesAsync();

        Response.Cookies.Delete("refreshToken");

        return Ok(new { message = "Token revoked" });
    }


    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
    {
        var users = await _context.Users
            .Select(u => new UserDto
            {
                Email = u.Email,
                Role = u.Role
            })
            .ToListAsync();

        return Ok(users);
    }
}

