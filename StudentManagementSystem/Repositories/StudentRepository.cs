using Microsoft.EntityFrameworkCore;
using StudentManagementAPI.Data;
using StudentManagementSystem.DTOs.Common;
using StudentManagementSystem.DTOs.Students;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Repositories
{
    public class StudentRepository : IStudentRepository
    {
        private readonly AppDbContext _context;

        public StudentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Student>> GetAllAsync()
        {
            return await _context.Students
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<PagedResult<Student>> GetPagedAsync(StudentQueryParameters queryParams)
        {
            var query = _context.Students.AsNoTracking().AsQueryable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
            {
                var term = queryParams.SearchTerm.Trim().ToLower();
                query = query.Where(s => s.Name.ToLower().Contains(term)
                                      || s.Email.ToLower().Contains(term)
                                      || s.Course.ToLower().Contains(term));
            }

            // Course filter
            if (!string.IsNullOrWhiteSpace(queryParams.Course))
            {
                var course = queryParams.Course.Trim().ToLower();
                query = query.Where(s => s.Course.ToLower() == course);
            }

            // Active status filter
            if (queryParams.IsActive.HasValue)
            {
                query = query.Where(s => s.IsActive == queryParams.IsActive.Value);
            }

            // Total count before paging
            var totalCount = await query.CountAsync();

            // Sorting
            query = (queryParams.SortBy?.ToLower(), queryParams.IsAscending) switch
            {
                ("age", true) => query.OrderBy(s => s.Age),
                ("age", false) => query.OrderByDescending(s => s.Age),
                ("course", true) => query.OrderBy(s => s.Course),
                ("course", false) => query.OrderByDescending(s => s.Course),
                ("createddate", true) => query.OrderBy(s => s.CreatedDate),
                ("createddate", false) => query.OrderByDescending(s => s.CreatedDate),
                ("name", false) => query.OrderByDescending(s => s.Name),
                _ => query.OrderBy(s => s.Name)
            };

            // Paging
            var skip = (queryParams.PageNumber - 1) * queryParams.PageSize;
            var items = await query.Skip(skip).Take(queryParams.PageSize).ToListAsync();

            return new PagedResult<Student>(items, totalCount, queryParams.PageNumber, queryParams.PageSize);
        }

        public async Task<Student?> GetByIdAsync(int id)
        {
            return await _context.Students.FindAsync(id);
        }

        public async Task<Student?> GetByEmailAsync(string email)
        {
            return await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Email.ToLower() == email.Trim().ToLower());
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Students.AnyAsync(s => s.Id == id);
        }

        public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
        {
            var normalizedEmail = email.Trim().ToLower();
            return await _context.Students.AnyAsync(s =>
                s.Email.ToLower() == normalizedEmail && (!excludeId.HasValue || s.Id != excludeId.Value));
        }

        public async Task<Student> AddAsync(Student student)
        {
            student.CreatedDate = DateTime.UtcNow;
            await _context.Students.AddAsync(student);
            await _context.SaveChangesAsync();
            return student;
        }

        public async Task<Student> UpdateAsync(Student student)
        {
            student.UpdatedDate = DateTime.UtcNow;
            _context.Students.Update(student);
            await _context.SaveChangesAsync();
            return student;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return false;
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Dictionary<string, int>> GetCourseStatisticsAsync()
        {
            return await _context.Students
                .AsNoTracking()
                .GroupBy(s => s.Course)
                .Select(g => new { Course = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Course, x => x.Count);
        }
    }
}
