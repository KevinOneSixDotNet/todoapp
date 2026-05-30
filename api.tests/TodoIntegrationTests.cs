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

public class TodoApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:Key", "integration-test-secret-key-minimum-32-chars!");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connection));
        });
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await base.DisposeAsync();
    }
}

public class TodoIntegrationTests(TodoApiFactory factory) : IClassFixture<TodoApiFactory>
{
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

    [Fact]
    public async Task UserA_CannotReadOrDelete_UserBsTodo()
    {
        var clientA = await CreateAuthorisedClientAsync("userA");
        var clientB = await CreateAuthorisedClientAsync("userB");

        var createResponse = await clientB.PostAsJsonAsync("/api/todos", new
        {
            title = "User B's private task",
            description = (string?)null,
            dueDate = DateTime.UtcNow.AddDays(7)
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var todoJson = await createResponse.Content.ReadAsStringAsync();
        var todoId = JsonDocument.Parse(todoJson).RootElement.GetProperty("id").GetString()!;

        var getResponse = await clientA.GetAsync($"/api/todos/{todoId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        var deleteResponse = await clientA.DeleteAsync($"/api/todos/{todoId}");
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);

        var ownerGetResponse = await clientB.GetAsync($"/api/todos/{todoId}");
        Assert.Equal(HttpStatusCode.OK, ownerGetResponse.StatusCode);
    }

    [Fact]
    public async Task CreateTodo_WithEmptyTitle_Returns400BadRequest()
    {
        var client = await CreateAuthorisedClientAsync("validationUser");

        var response = await client.PostAsJsonAsync("/api/todos", new
        {
            title = "",
            description = (string?)null,
            dueDate = DateTime.UtcNow.AddDays(1)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("errors", out _),
            "Response body should contain a ProblemDetails 'errors' object.");
    }
}
