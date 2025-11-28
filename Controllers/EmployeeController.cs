using EmployeeDirectoryApp.Data;
using EmployeeDirectoryApp.DTO;
using EmployeeDirectoryApp.Models.Entities;
using EmployeeDirectoryApp.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeDirectoryApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class EmployeeController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EmployeeController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.Employees
                .Include(e => e.Department)
                .Select(e => new EmployeeResponseDTO
                {
                    EmployeeId = e.EmployeeId,
                    FullName = (e.FirstName ?? "") + " " + (e.LastName ?? ""),
                    Email = e.Email,
                    PhoneNo = e.PhoneNo,
                    Gender = e.Gender,
                    JobRole = e.JobRole,
                    DepartmentName = e.Department != null ? e.Department.Name : null
                })
                .ToListAsync();

            return Ok(list);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmployeeById(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .Where(e => e.EmployeeId == id)
                .Select(e => new EmployeeResponseDTO
                {
                    EmployeeId = e.EmployeeId,
                    FullName = e.FirstName + " " + e.LastName,
                    Email = e.Email,
                    PhoneNo = e.PhoneNo,
                    Gender = e.Gender,
                    JobRole = e.JobRole,
                    DepartmentName = e.Department != null ? e.Department.Name : null
                })
                .FirstOrDefaultAsync();

            if (employee == null) return NotFound("Employee not found");

            return Ok(employee);
        }

        [HttpGet("byName")]
        public async Task<IActionResult> GetByName(string name)
        {
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Where(e => e.FirstName.Contains(name) || e.LastName.Contains(name))
                .Select(e => new EmployeeResponseDTO
                {
                    EmployeeId = e.EmployeeId,
                    FullName = e.FirstName + " " + e.LastName,
                    Email = e.Email,
                    PhoneNo = e.PhoneNo,
                    Gender = e.Gender,
                    JobRole = e.JobRole,
                    DepartmentName = e.Department.Name
                })
                .ToListAsync();
            return Ok(employees);
        }

        
        [HttpGet("byJobRole")]
        public async Task<IActionResult> GetByJobRole(string jobRole)
        {
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Where(e => e.JobRole == jobRole)
                .Select(e => new EmployeeResponseDTO
                {
                    EmployeeId = e.EmployeeId,
                    FullName = e.FirstName + " " + e.LastName,
                    Email = e.Email,
                    PhoneNo = e.PhoneNo,
                    Gender = e.Gender,
                    JobRole = e.JobRole,
                    DepartmentName = e.Department.Name
                })
                .ToListAsync();
            return Ok(employees);
        }

       
        //[HttpPost("add")]
        //[Authorize(Roles ="Admin")]
        //public async Task<IActionResult> AddEmployee([FromBody] CreateEmployeeDto dto)
        //{
        //    var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        //    if (role != "Admin")
        //        return Forbid("Only admin can add to records");
        //    var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        //    var existingEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == dto.Email);

        //    if (existingUser != null && existingEmployee != null)
        //        return BadRequest("User with this email already exists as an employee.");

        //    User user;
        //    if (existingUser == null)
        //    {
        //        var defaultPassword = dto.FirstName + "@123";
        //        user = new User
        //        {
        //            Email = dto.Email,
        //            PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
        //            Role = "Employee",
        //            MustChangePassword = true
        //        };
        //        _context.Users.Add(user);
        //        await _context.SaveChangesAsync();
        //    }
        //    else
        //    {
        //        user = existingUser;
        //    }

        //    if (existingEmployee == null)
        //    {
        //        var employee = new Employee
        //        {
        //            FirstName = dto.FirstName,
        //            LastName = dto.LastName,
        //            Email = dto.Email,
        //            Gender = dto.Gender,
        //            JobRole = dto.JobRole,
        //            DepartmentId = dto.DepartmentId,
        //            PhoneNo = dto.PhoneNo,
        //            UserId = user.UserId
        //        };
        //        _context.Employees.Add(employee);
        //        await _context.SaveChangesAsync();
        //    }

        //    return Ok(new
        //    {
        //        Message = "Employee and user account created successfully",
        //        DefaultPassword = existingUser == null ? dto.FirstName + "@123" : "Already has a password"
        //    });
        //}
        // PUT: api/Employee/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEmployee(int id, EmployeeUpdateDto dto)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound("Employee not found");

            employee.FirstName = dto.FirstName;
            employee.LastName = dto.LastName;
            employee.Email = dto.Email;
            employee.PhoneNo = dto.PhoneNo;
            employee.Gender = dto.Gender;
            employee.JobRole = dto.JobRole;
            employee.DepartmentId = dto.DepartmentId;

            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();
            return Ok(employee);
        }

        // DELETE: api/Employee/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound("Employee not found");

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
            return Ok("Employee deleted");
        }
    }
}