using TaskManagerAPI.Models;

namespace TaskManagerAPI.Repositories
{
    public interface ITaskRepository
    {
        IEnumerable<TaskItem> GetAllForUser(int userId, string? search, TaskState? status, string? sortBy, bool ascending, int page, int pageSize);
        TaskItem? GetById(int id, int userId);
        void Create(TaskItem task);
        void Update(TaskItem updatedTask);
        void Delete(int id, int userId);
    }
}
