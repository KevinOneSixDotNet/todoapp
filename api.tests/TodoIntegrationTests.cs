using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FunctionTodo.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FunctionTodo.Api.Tests;

// ── Test Factory ────────────────────────────────────────────────────────────
// Uses a single persistent SqliteConnection for the lifetime of the test run.
// A fresh :memory: database is destroyed the moment its connection closes, so
// we keep one connection open and share it across all scopes via AddDbContext.
public class TodoApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Provide a test JWT key so the API starts without requiring config files
        builder.UseSetting("Jwt:Key", "integration-test-secret-key-minimum-32-chars!");

        builder.ConfigureServices(services =>
        {
            // Replace the real SQLite file DbContext with our in-memory connection
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connection));
        });
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();

        // Accessing Services builds the host, which runs Program.cs startup
        // (including db.Database.Migrate()). EnsureCreatedAsync is a no-op if
        // Migrate already created the schema; it's a safety net for future changes.
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    // 'new' hides the sync Dispose on the base class; disposes both connection and host
    public new async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await base.DisposeAsync();
    }
}

// ── Integration Tests ───────────────────────────────────────────────────────
public class TodoIntegrationTests(TodoApiFactory factory) : IClassFixture<TodoApiFactory>
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    // Returns a new HttpClient already authorised with a freshly registered user's JWT.
    // GUID suffix prevents username collisions across parallel test runs.
    private async Task<HttpClient> CreateAuthorisedClientAsync(string userPrefix)
    {
        var username = $"{userPrefix}_{Guid.NewGuid():N}";
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            password = "Password123!"
        });
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var token = JsonDocument.Parse(json).RootElement.GetProperty("token").GetString()!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // ── Test 1: Ownership / Data Isolation ─────────────────────────────────
    // Verifies that a user cannot read or delete another user's todo.
    // A correct implementation returns 404 (not 403) because leaking the existence
    // of a resource is itself an information disclosure — the controller's
    // FirstOrDefaultAsync scopes the WHERE clause to the caller's UserId.
    [Fact]
    public async Task UserA_CannotReadOrDelete_UserBsTodo()
    {
        var clientA = await CreateAuthorisedClientAsync("userA");
        var clientB = await CreateAuthorisedClientAsync("userB");

        // User B creates a todo
        var createResponse = await clientB.PostAsJsonAsync("/api/todos", new
        {
            title = "User B's private task",
            description = (string?)null,
            dueDate = DateTime.UtcNow.AddDays(7)
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var todoJson = await createResponse.Content.ReadAsStringAsync();
        var todoId = JsonDocument.Parse(todoJson).RootElement.GetProperty("id").GetString()!;

        // User A tries to GET User B's todo — must be 404
        var getResponse = await clientA.GetAsync($"/api/todos/{todoId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        // User A tries to DELETE User B's todo — must also be 404
        var deleteResponse = await clientA.DeleteAsync($"/api/todos/{todoId}");
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);

        // Confirm User B can still read their own todo (data is intact)
        var ownerGetResponse = await clientB.GetAsync($"/api/todos/{todoId}");
        Assert.Equal(HttpStatusCode.OK, ownerGetResponse.StatusCode);
    }

    // ── Test 2: Input Validation ────────────────────────────────────────────
    // Verifies that posting a todo with a blank Title returns 400 Bad Request
    // with a ProblemDetails body (standard ASP.NET Core validation response).
    [Fact]
    public async Task CreateTodo_WithEmptyTitle_Returns400BadRequest()
    {
        var client = await CreateAuthorisedClientAsync("validationUser");

        var response = await client.PostAsJsonAsync("/api/todos", new
        {
            title = "",          // violates [Required] — empty string is treated as missing
            description = (string?)null,
            dueDate = DateTime.UtcNow.AddDays(1)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Response body must be RFC 7807 ProblemDetails, not a generic error string
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("errors", out _),
            "Response body should contain a ProblemDetails 'errors' object.");
    }
}
