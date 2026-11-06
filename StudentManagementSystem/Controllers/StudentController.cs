using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagementSystem.DTOs.Common;
using StudentManagementSystem.DTOs.Students;
using StudentManagementSystem.Services;

namespace StudentManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentController : ControllerBase
    {
        private readonly IStudentService _service;

        public StudentController(IStudentService service)
        {
            _service = service;
        }

        /// <summary>
        /// Retrieves a paginated and filterable list of students.
        /// </summary>
        [Authorize]
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<StudentResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] StudentQueryParameters queryParams)
        {
            var response = await _service.GetAllAsync(queryParams);
            return Ok(response);
        }

        /// <summary>
        /// Retrieves a single student by their ID.
        /// </summary>
        [Authorize]
        [HttpGet("{id:int}", Name = nameof(GetById))]
        [ProducesResponseType(typeof(ApiResponse<StudentResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<StudentResponseDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _service.GetByIdAsync(id);
            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Retrieves statistical breakdown of students per course.
        /// </summary>
        [Authorize]
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(ApiResponse<Dictionary<string, int>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStatistics()
        {
            var response = await _service.GetStatisticsAsync();
            return Ok(response);
        }

        /// <summary>
        /// Adds a new student record (Admin and Teacher only).
        /// </summary>
        [Authorize(Roles = "Admin,Teacher")]
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<StudentResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<StudentResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Add([FromBody] StudentCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                return BadRequest(ApiResponse<StudentResponseDto>.FailureResult("Validation failed.", errors));
            }

            var response = await _service.AddAsync(dto);
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return CreatedAtAction(nameof(GetById), new { id = response.Data!.Id }, response);
        }

        /// <summary>
        /// Updates an existing student record (Admin and Teacher only).
        /// </summary>
        [Authorize(Roles = "Admin,Teacher")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<StudentResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<StudentResponseDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<StudentResponseDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] StudentUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                return BadRequest(ApiResponse<StudentResponseDto>.FailureResult("Validation failed.", errors));
            }

            var response = await _service.UpdateAsync(id, dto);
            if (!response.Success)
            {
                if (response.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(response);
                }
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Deletes a student record (Admin only).
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _service.DeleteAsync(id);
            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }
    }
}
