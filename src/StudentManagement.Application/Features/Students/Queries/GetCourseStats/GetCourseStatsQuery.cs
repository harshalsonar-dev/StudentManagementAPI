using MediatR;
using StudentManagement.Application.Common.Models;
using StudentManagement.Domain.Interfaces;

namespace StudentManagement.Application.Features.Students.Queries.GetCourseStats
{
    public record GetCourseStatsQuery : IRequest<ApiResponse<Dictionary<string, int>>>;

    public class GetCourseStatsQueryHandler : IRequestHandler<GetCourseStatsQuery, ApiResponse<Dictionary<string, int>>>
    {
        private readonly IStudentRepository _studentRepository;

        public GetCourseStatsQueryHandler(IStudentRepository studentRepository)
        {
            _studentRepository = studentRepository;
        }

        public async Task<ApiResponse<Dictionary<string, int>>> Handle(GetCourseStatsQuery request, CancellationToken cancellationToken)
        {
            var stats = await _studentRepository.GetCourseStatisticsAsync(cancellationToken);
            return ApiResponse<Dictionary<string, int>>.SuccessResult(stats, "Course statistics retrieved successfully.");
        }
    }
}
