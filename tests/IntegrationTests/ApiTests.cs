using System.Text;
using System.Text.Json;
using SlimTodoApp.Api.Application.Requests;
using SlimTodoApp.Api.Data;
using SlimTodoApp.Api.Domain.Models;

namespace SlimTodoApp.IntegrationTests;

public class ApiTests : IClassFixture<IntegrationTestsApplicationFactory>
{
    private readonly IntegrationTestsApplicationFactory _factory;

    public ApiTests(IntegrationTestsApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GivenListOfTodosExist_WhenListAllRequestReceived_ThenReturnAllTodosAndStatusOk()
    {
        // Given
        int totalTodos = 5;
        using var scope = _factory.Services.CreateScope();
        var client = _factory.CreateClient();
        var context = scope.ServiceProvider.GetRequiredService<TodoContext>();

        for (int i = 0; i < totalTodos; i++)
        {
            context.Todos.Add(new Todo($"task num {i + 1}"));
        }

        context.SaveChanges();

        // When
        var result = await client.GetAsync("/todos");

        // Then
        Assert.True(result.IsSuccessStatusCode);
        var content = await result.Content.ReadFromJsonAsync<IEnumerable<Todo>>();

        Assert.NotNull(content);
        var savedTodos = context.Todos.Count();
        Assert.Equal(savedTodos, content!.Count());
    }

    [Fact]
    public async Task GivenTodoExist_WhenIdIsRequested_ThenReturTodoAndStatusOk()
    {
        // Given
        using var scope = _factory.Services.CreateScope();
        var client = _factory.CreateClient();
        var context = scope.ServiceProvider.GetRequiredService<TodoContext>();
        var todo = new Todo("A very specific task");

        context.Todos.Add(todo);
        context.SaveChanges();

        // When
        var result = await client.GetAsync($"/todos/{todo.Id}");

        // Then
        Assert.True(result.IsSuccessStatusCode);
        var content = await result.Content.ReadFromJsonAsync<Todo>();
        Assert.NotNull(content);
        Assert.Equal(todo.Id, content.Id);
    }

    [Fact]
    public async Task GivenValidCreateTaskRequest_WhenRequestReceived_ThenCreateNewTaskAndReturnCreatedStatus()
    {
        // Given
        using var scope = _factory.Services.CreateScope();
        var client = _factory.CreateClient();
        var context = scope.ServiceProvider.GetRequiredService<TodoContext>();

        var requestBody = new CreateTaskRequest { Title = "Buy coffee" };

        // When
        var result = await client.PostAsync("/todos", CreateRequestBody(requestBody));

        // Then
        Assert.True(result.IsSuccessStatusCode);
        Assert.Equal(StatusCodes.Status201Created, (int)result.StatusCode);
        var createdTask = context.Todos.FirstOrDefault(x => x.Title == requestBody.Title);
        Assert.NotNull(createdTask);
    }

    [Fact]
    public async Task GivenRequestExists_WhenCompletionIsRequested_ThenUpdateStatusAndReturnNoContent()
    {
        // Given
        using var scope = _factory.Services.CreateScope();
        var client = _factory.CreateClient();
        var context = scope.ServiceProvider.GetRequiredService<TodoContext>();

        var todo = new Todo("Almost done... Done!");

        context.Todos.Add(todo);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        // When
        var result = await client.PatchAsync($"/todos/{todo.Id}/complete", null);

        // Then
        Assert.True(result.IsSuccessStatusCode);
        Assert.Equal(StatusCodes.Status204NoContent, (int)result.StatusCode);
        var completedTask = context.Todos.First(x => x.Id == todo.Id);
        Assert.True(completedTask.Completed);
    }

    [Theory]
    [MemberData(nameof(CreateFilterRequests))]
    public async Task GivenListOfTodosExist_WhenFilterRequestIsReceived_ThenReturnMatchingTodosAndStatusOk(GetTodosByFilterRequest filter, int expectedCount)
    {
        // Given
        using var scope = _factory.Services.CreateScope();
        var client = _factory.CreateClient();
        var context = scope.ServiceProvider.GetRequiredService<TodoContext>();

        for (int i = 0; i < 5; i++)
        {
            context.Todos.Add(new Todo($"task num {i + 1}"));
        }

        var completedTodo = new Todo("This is done");
        completedTodo.Complete();
        context.Todos.Add(completedTodo);

        context.SaveChanges();

        // When
        var result = await client.PostAsync("/todos/filter", CreateRequestBody(filter));

        // Then
        Assert.True(result.IsSuccessStatusCode);
        var content = await result.Content.ReadFromJsonAsync<IEnumerable<Todo>>();

        Assert.NotNull(content);
        Assert.Equal(expectedCount, content.Count());
        context.Todos.RemoveRange(context.Todos);
        context.SaveChanges();
    }

    public static IEnumerable<object[]> CreateFilterRequests()
    {
        var filter = new GetTodosByFilterRequest
        {
            Title = "3"
        };
        int expectedCount = 1;
        yield return new object[] { filter, expectedCount };

        filter = new GetTodosByFilterRequest { Completed = true };
        yield return new object[] { filter, expectedCount };

        filter = new GetTodosByFilterRequest { DateFrom = DateTime.Now.AddHours(-1) };
        expectedCount = 6;

        yield return new object[] { filter, expectedCount };

        filter = new GetTodosByFilterRequest { DateTo = DateTime.Now.AddHours(-1) };
        expectedCount = 0;
        yield return new object[] { filter, expectedCount };
    }

    private static StringContent CreateRequestBody(object body)
    {
        return new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
    }
}