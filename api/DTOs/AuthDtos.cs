using System.ComponentModel.DataAnnotations;

namespace FunctionTodo.Api.DTOs;

public record AuthRequest(
    [Required][MinLength(3)][MaxLength(50)] string Username,
    [Required][MinLength(6)] string Password
);

public record AuthResponse(string Token, string Username);
