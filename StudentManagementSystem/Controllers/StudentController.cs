using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagementSystem.Models;

[ApiController]
[Route("api/[controller]")]
public class StudentController : ControllerBase
{
    private readonly IStudentService _service;

    public StudentController(IStudentService service)
    {
        _service = service;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAll());
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Add(Student student)
    {
        await _service.Add(student);
        return Ok("Student Added");
    }

    [Authorize]
    [HttpPut]
    public async Task<IActionResult> Update(Student student)
    {
        await _service.Update(student);
        return Ok("Student Updated");
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.Delete(id);
        return Ok("Student Deleted");
    }
}