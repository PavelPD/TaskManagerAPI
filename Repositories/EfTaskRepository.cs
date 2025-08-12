using Microsoft.Extensions.Caching.Memory;
using TaskManagerAPI.Data;
using TaskManagerAPI.Models;

namespace TaskManagerAPI.Repositories
{
    public class EfTaskRepository : ITaskRepository
    {
        private readonly TaskDbContext _context;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "task_list";

        public EfTaskRepository(TaskDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public IEnumerable<TaskItem> GetAllForUser(int userId, string? search, TaskState? status, string? sortBy, bool ascending, int page, int pageSize)
        {
            var cacheKey = $"{CacheKey}_{search}_{status}_{sortBy}_{ascending}_{page}_{pageSize}";

            if (!_cache.TryGetValue(cacheKey, out IEnumerable<TaskItem> tasks))
            {
                IQueryable<TaskItem> query = _context.Tasks.Where(t => t.UserId == userId);

                //фильтрация
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(t => t.Title.Contains(search) || t.Description.Contains(search));

                if (status.HasValue)
                    query = query.Where(t => t.Status == status.Value);

                //сортировка
                query = sortBy?.ToLower() switch
                {
                    "title" => ascending ?
                        query.OrderBy(t => t.Title) :
                        query.OrderByDescending(t => t.Title),
                    "created" => ascending ?
                        query.OrderBy(t => t.Id) :
                        query.OrderByDescending(t => t.Id),
                    "status" => ascending ?
                        query.OrderBy(t => t.Status) :
                        query.OrderByDescending(t => t.Status),
                    _ => query
                };

                //пагинация
                query = query.Skip((page - 1) * pageSize).Take(pageSize);

                tasks = query.ToList();

                //кэшируем на 30 секунд
                _cache.Set(cacheKey, tasks, TimeSpan.FromSeconds(30));
            }

            return tasks;
        }

        public TaskItem? GetById(int id, int userId)
        {
            return _context.Tasks.FirstOrDefault(t => t.Id == id && t.UserId == userId);
        }

        public void Create(TaskItem task)
        {
            _context.Tasks.Add(task);
            _context.SaveChanges();
            _cache.Remove(CacheKey);
        }

        public void Update(TaskItem updatedTask)
        {
            var existing = _context.Tasks.Find(updatedTask.Id);
            if (existing == null)
                throw new InvalidOperationException($"Task with id {updatedTask.Id} not found");

            existing.Title = updatedTask.Title;
            existing.Description = updatedTask.Description;
            existing.Status = updatedTask.Status;

            _context.SaveChanges();
            _cache.Remove(CacheKey);
        }

        public void Delete(int id, int userId)
        {
            var task = _context.Tasks.FirstOrDefault(t => t.Id == id && t.UserId == userId);
            if (task != null)
            {
                _context.Tasks.Remove(task);
                _context.SaveChanges();
                _cache.Remove(CacheKey);
            }
        }
    }
}
