using Microsoft.EntityFrameworkCore;
using StudentManagement.Domain.Entities;
using StudentManagement.Domain.Interfaces;
using StudentManagement.Infrastructure.Data;

namespace StudentManagement.Infrastructure.Repositories
{
    public class StudentRepository : GenericRepository<Student>, IStudentRepository
    {
        public StudentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<(IReadOnlyList<Student> Items, int TotalCount)> GetPagedStudentsAsync(
            string? searchTerm,
            string? course,
            bool? isActive,
            string? sortBy,
            bool isAscending,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(s => s.Name.ToLower().Contains(term)
                                      || s.Email.ToLower().Contains(term)
                                      || s.Course.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(course))
            {
                var c = course.Trim().ToLower();
                query = query.Where(s => s.Course.ToLower() == c);
            }

            if (isActive.HasValue)
            {
                query = query.Where(s => s.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            query = (sortBy?.ToLower(), isAscending) switch
            {
                ("age", true) => query.OrderBy(s => s.Age),
                ("age", false) => query.OrderByDescending(s => s.Age),
                ("course", true) => query.OrderBy(s => s.Course),
                ("course", false) => query.OrderByDescending(s => s.Course),
                ("createddate", true) => query.OrderBy(s => s.CreatedDate),
                ("createddate", false) => query.OrderByDescending(s => s.CreatedDate),
                ("name", false) => query.OrderByDescending(s => s.Name),
                (_, false) => query.OrderByDescending(s => s.Name),
                _ => query.OrderBy(s => s.Name)
            };

            var skip = (pageNumber - 1) * pageSize;
            var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<Dictionary<string, int>> GetCourseStatisticsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .GroupBy(s => s.Course)
                .Select(g => new { Course = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Course, x => x.Count, cancellationToken);
        }
    }
}
