using System.ComponentModel.DataAnnotations;
using FunctionTodo.Api.Validation;

namespace FunctionTodo.Api.DTOs;

public record TodoRequest(
    [Required][MaxLength(100)] string Title,
    [MaxLength(2000)] string? Description,
    [ValidDueDate] DateTime DueDate
);

public record TodoUpdateRequest(
    [Required][MaxLength(100)] string Title,
    [MaxLength(2000)] string? Description,
    [ValidDueDate] DateTime DueDate,
    bool IsComplete
);

public record TodoResponse(
    Guid Id,
    string Title,
    string? Description,
    DateTime DueDate,
    bool IsComplete
);
