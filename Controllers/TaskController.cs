using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagerAPI.Models;
using TaskManagerAPI.Repositories;

namespace TaskManagerAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TaskController : ControllerBase
    {
        private readonly ITaskRepository _repository;
        private readonly IMapper _mapper;

        public TaskController(ITaskRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        private int GetUserId()
        {
            return int.Parse(User.Claims.First(c => c.Type == "id").Value);
        }

        [HttpGet("test-error")]
        public IActionResult TestError()
        {
            throw new Exception("Тестовая ошибка для проверки middleware");
        }

        [HttpGet]
        public ActionResult<IEnumerable<TaskItem>> GetAll(
            string? search,
            TaskState? status,
            string? sortBy = "created",
            string? sortOrder = "asc",
            int page = 1,
            int pageSize = 10)
        {
            bool ascending = sortOrder?.ToLower() != "desc";
            int userId = GetUserId();

            var tasks = _repository.GetAllForUser(userId, search, status, sortBy, ascending, page, pageSize);

            return Ok(tasks);
        }

        [HttpGet("{id}")]
        public ActionResult<TaskItem> GetById(int id)
        {
            int userId = GetUserId();
            var task = _repository.GetById(id, userId);
            return task == null ? NotFound() : Ok(task);
        }

        [HttpPost]
        public IActionResult Create(CreateTaskDto dto)
        {            
            var task = _mapper.Map<TaskItem>(dto);

            //привязка пользователя
            task.UserId = GetUserId();

            _repository.Create(task);
            return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, UpdateTaskDto dto)
        {
            if (id != dto.Id)
                return BadRequest("ID mismatch");
            
            int userId = GetUserId();
            var existing = _repository.GetById(id, userId);

            if (existing == null)
                return NotFound();

            _mapper.Map(dto, existing);

            _repository.Update(existing);
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            int userId = GetUserId();
            var existing = _repository.GetById(id, userId);

            if (existing == null)
                return NotFound();

            _repository.Delete(id, userId);
            return NoContent();
        }        
    }
}
