using System.ComponentModel.DataAnnotations;

namespace TaskManagerAPI.Models
{
    public class CreateTaskDto
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Title { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public TaskState Status { get; set; } = TaskState.NotStarted;
    }
}
