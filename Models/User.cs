namespace TaskManagerAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = "User";


        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}
