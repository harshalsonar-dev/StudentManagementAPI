using StudentManagementSystem.Models;

public class StudentService : IStudentService
{
    private readonly IStudentRepository _repo;

    public StudentService(IStudentRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<Student>> GetAll() => await _repo.GetAll();
    public async Task<Student> GetById(int id) => await _repo.GetById(id);
    public async Task Add(Student student) => await _repo.Add(student);
    public async Task Update(Student student) => await _repo.Update(student);
    public async Task Delete(int id) => await _repo.Delete(id);
}