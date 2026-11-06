using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentManagement.API.Extensions;
using StudentManagement.Application.Common.Models;
using StudentManagement.Application.DTOs.Students;
using StudentManagement.Application.Features.Students.Commands.CreateStudent;
using StudentManagement.Application.Features.Students.Commands.DeleteStudent;
using StudentManagement.Application.Features.Students.Commands.UpdateStudent;
using StudentManagement.Application.Features.Students.Queries.GetCourseStats;
using StudentManagement.Application.Features.Students.Queries.GetStudentById;
using StudentManagement.Application.Features.Students.Queries.GetStudentsPaged;
using StudentManagement.Domain.Enums;

namespace StudentManagement.API.Controllers
{
    [EnableRateLimiting(RateLimitingExtensions.GeneralPolicy)]
    public class StudentsController : ApiControllerBase
    {
        /// <summary>
        /// Retrieves paginated, filtered, and sorted students list.
        /// </summary>
        [Authorize]
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<StudentDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] PaginationParameters parameters)
        {
            var response = await Mediator.Send(new GetStudentsPagedQuery(parameters));
            return Ok(response);
        }

        /// <summary>
        /// Retrieves a single student by ID.
        /// </summary>
        [Authorize]
        [HttpGet("{id:int}", Name = nameof(GetById))]
        [ProducesResponseType(typeof(ApiResponse<StudentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await Mediator.Send(new GetStudentByIdQuery(id));
            return Ok(response);
        }

        /// <summary>
        /// Retrieves student count statistics grouped by course.
        /// </summary>
        [Authorize]
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(ApiResponse<Dictionary<string, int>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStatistics()
        {
            var response = await Mediator.Send(new GetCourseStatsQuery());
            return Ok(response);
        }

        /// <summary>
        /// Creates a new student record (Admin and Teacher only).
        /// </summary>
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Teacher}")]
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<StudentDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] CreateStudentDto dto)
        {
            var response = await Mediator.Send(new CreateStudentCommand(dto));
            return CreatedAtAction(nameof(GetById), new { id = response.Data!.Id }, response);
        }

        /// <summary>
        /// Updates an existing student record (Admin and Teacher only).
        /// </summary>
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Teacher}")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<StudentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateStudentDto dto)
        {
            var response = await Mediator.Send(new UpdateStudentCommand(id, dto));
            return Ok(response);
        }

        /// <summary>
        /// Deletes a student record (Admin only).
        /// </summary>
        [Authorize(Roles = AppRoles.Admin)]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await Mediator.Send(new DeleteStudentCommand(id));
            return Ok(response);
        }
    }
}
