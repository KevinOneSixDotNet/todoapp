using System.Security.Claims;
using FunctionTodo.Api.Data;
using FunctionTodo.Api.DTOs;
using FunctionTodo.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FunctionTodo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TodosController(AppDbContext db) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var todos = await db.Todos
            .Where(t => t.UserId == UserId)
            .OrderBy(t => t.IsComplete)
            .ThenBy(t => t.DueDate)
            .Select(t => new TodoResponse(t.Id, t.Title, t.Description, t.DueDate, t.IsComplete))
            .ToListAsync();

        return Ok(todos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == UserId);
        if (todo is null) return NotFound();
        return Ok(new TodoResponse(todo.Id, todo.Title, todo.Description, todo.DueDate, todo.IsComplete));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TodoRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var todo = new Todo
        {
            UserId = UserId,
            Title = req.Title,
            Description = req.Description,
            DueDate =  req.DueDate,
        };
        db.Todos.Add(todo);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = todo.Id },
            new TodoResponse(todo.Id, todo.Title, todo.Description, todo.DueDate, todo.IsComplete));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TodoUpdateRequest req)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == UserId);
        if (todo is null) return NotFound();

        todo.Title = req.Title;
        todo.Description = req.Description;
        todo.DueDate = req.DueDate;
        todo.IsComplete = req.IsComplete;

        await db.SaveChangesAsync();
        return Ok(new TodoResponse(todo.Id, todo.Title, todo.Description, todo.DueDate, todo.IsComplete));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == UserId);
        if (todo is null) return NotFound();

        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
