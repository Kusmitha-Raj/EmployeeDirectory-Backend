using EmployeeDirectoryApp.Data;
using EmployeeDirectoryApp.DTO;
using EmployeeDirectoryApp.Models.Entities;
using EmployeeDirectoryApp.Repository;
using EmployeeDirectoryApp.Services;
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
        private readonly IDepartmentService _departmentService;

        public DepartmentController(IDepartmentService departmentService)
        {
            _departmentService = departmentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDepartments()
        {
            var departments = await _departmentService.GetAllAsync();
            return Ok(departments);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDepartmentById(int id)
        {
            var department = await _departmentService.GetByIdAsync(id);
            if (department == null) return NotFound("Department not found");
            return Ok(department);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddDepartment(DepartmentDto dto)
        {
            try
            {
                var department = await _departmentService.AddAsync(dto);
                return Ok(department);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateDepartment(int id, DepartmentUpdateDTO dto)
        {
            var updated = await _departmentService.UpdateAsync(id, dto);
            if (updated == null) return NotFound("Department not found");
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var success = await _departmentService.DeleteAsync(id);
            if (!success) return NotFound("Department not found");
            return Ok("Department deleted");
        }
    }
}