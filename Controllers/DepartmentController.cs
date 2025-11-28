using EmployeeDirectoryApp.Data;
using EmployeeDirectoryApp.DTO;
using EmployeeDirectoryApp.Models.Entities;
using EmployeeDirectoryApp.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace EmployeeDirectoryApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DepartmentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DepartmentController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Department
        [HttpGet]
        public async Task<IActionResult> GetAllDepartments()
        {
            var departments = await _context.Departments
                .Select(d => new DepartmentResponseDTO
                {
                    DepartmentId = d.DepartmentId,
                    Name = d.Name
                }).ToListAsync();
            return Ok(departments);
        }

        // GET: api/Department/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDepartmentById(int id)
        {
            var d = await _context.Departments.FindAsync(id);
            if (d == null) return NotFound("Department not found");

            return Ok(new DepartmentResponseDTO
            {
                DepartmentId = d.DepartmentId,
                Name = d.Name
            });
        }

        // POST: api/Department
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddDepartment(DepartmentDto dto)
        {
            var existingDept = await _context.Departments
                .FirstOrDefaultAsync(d => d.Name.ToLower() == dto.Name.ToLower());
            if (existingDept != null)
                return BadRequest("Department with this name already exists.");

            var department = new Department { Name = dto.Name };
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
            return Ok(department);
        }

        // PUT: api/Department/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateDepartment(int id, DepartmentUpdateDTO dto)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null) return NotFound("Department not found");

            department.Name = dto.Name;
            _context.Departments.Update(department);
            await _context.SaveChangesAsync();
            return Ok(department);
        }

        // DELETE: api/Department/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null) return NotFound("Department not found");

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();
            return Ok("Department deleted");
        }
    }
}
