using System.ComponentModel.DataAnnotations;

namespace FunctionTodo.Api.Models;

public class Todo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime DueDate { get; set; }

    public bool IsComplete { get; set; }

    public User User { get; set; } = null!;
}
