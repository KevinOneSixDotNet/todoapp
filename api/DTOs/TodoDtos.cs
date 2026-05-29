using System.ComponentModel.DataAnnotations;

namespace FunctionTodo.Api.DTOs;

public record TodoRequest(
    [Required][MaxLength(100)] string Title,
    string? Description,
    DateTime DueDate
);

public record TodoUpdateRequest(
    [Required][MaxLength(100)] string Title,
    string? Description,
    DateTime DueDate,
    bool IsComplete
);

public record TodoResponse(
    Guid Id,
    string Title,
    string? Description,
    DateTime DueDate,
    bool IsComplete
);
