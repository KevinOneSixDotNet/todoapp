using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FunctionTodo.Api.Data;
using FunctionTodo.Api.DTOs;
using FunctionTodo.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FunctionTodo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AppDbContext db, IConfiguration config) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AuthRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        if (await db.Users.AnyAsync(u => u.Username == req.Username))
            return Conflict(new { message = "Username is already taken." });

        var user = new User
        {
            Username = req.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Ok(new AuthResponse(GenerateToken(user), user.Username));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid username or password." });

        return Ok(new AuthResponse(GenerateToken(user), user.Username));
    }

    private string GenerateToken(User user)
    {
        var key = config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            ],
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
