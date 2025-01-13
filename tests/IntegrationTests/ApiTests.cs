using System.Text;
using System.Text.Json;
using SlimTodoApp.Api.Application.Requests;
using SlimTodoApp.Api.Data;
using SlimTodoApp.Api.Domain.Models;

namespace SlimTodoApp.IntegrationTests;

[Collection("DbCollection")]
public class ApiTests : IClassFixture<IntegrationTestsApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TodoContext _context;
    private readonly IServiceScope _scope;

    public ApiTests(IntegrationTestsApplicationFactory factory)
    {
        _scope = factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<TodoContext>();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GivenListOfTodosExist_WhenListAllRequestReceived_ThenReturnAllTodosAndStatusOk()
    {
        // Given
        int totalTodos = 5;

        List<Todo> todosToAdd = new(totalTodos);

        for (int i = 0; i < totalTodos; i++)
        {
            todosToAdd.Add(new Todo($"task num {i + 1}"));
        }
        
        _context.AddRange(todosToAdd);
        _context.SaveChanges();

        // When
        var result = await _client.GetAsync("/todos");

        // Then
        Assert.True(result.IsSuccessStatusCode);
        var content = await result.Content.ReadFromJsonAsync<IEnumerable<Todo>>();

        Assert.NotNull(content);
        Assert.Contains(content, x => todosToAdd.Any(t => t.Id == x.Id));
    }

    [Fact]
    public async Task GivenTodoExist_WhenIdIsRequested_ThenReturTodoAndStatusOk()
    {
        // Given
        var todo = new Todo("A very specific task");

        _context.Todos.Add(todo);
        _context.SaveChanges();

        // When
        var result = await _client.GetAsync($"/todos/{todo.Id}");

        // Then
        Assert.True(result.IsSuccessStatusCode);
        var content = await result.Content.ReadFromJsonAsync<Todo>();
        Assert.NotNull(content);
        Assert.Equal(todo.Id, content.Id);
    }

    [Theory]
    [InlineData("Buy coffee", null)]
    [InlineData("Buy coffee", "If the price is good, buy two packages")]
    public async Task GivenValidCreateTaskRequest_WhenRequestReceived_ThenCreateNewTaskAndReturnCreatedStatus(string title, string? body)
    {
        // Given
        var requestBody = new CreateTaskRequest(title, body!);

        // When
        var result = await _client.PostAsync("/todos", CreateRequestBody(requestBody));

        // Then
        Assert.True(result.IsSuccessStatusCode);
        Assert.Equal(StatusCodes.Status201Created, (int)result.StatusCode);
        
        var content = await result.Content.ReadFromJsonAsync<Todo>();
        Assert.NotNull(content);

        var createdTodo = _context.Todos.FirstOrDefault(x => x.Id == content.Id);
        Assert.NotNull(createdTodo);
    }

    [Fact]
    public async Task GivenRequestExists_WhenCompletionIsRequested_ThenUpdateStatusAndReturnNoContent()
    {
        // Given
        var todo = new Todo("Almost done... Done!");

        _context.Todos.Add(todo);
        _context.SaveChanges();
        _context.ChangeTracker.Clear();

        // When
        var result = await _client.PatchAsync($"/todos/{todo.Id}/complete", null);

        // Then
        Assert.True(result.IsSuccessStatusCode);
        Assert.Equal(StatusCodes.Status204NoContent, (int)result.StatusCode);
        var completedTask = _context.Todos.First(x => x.Id == todo.Id);
        Assert.True(completedTask.Completed);
    }

    [Theory]
    [MemberData(nameof(CreateFilterRequests))]
    public async Task GivenListOfTodosExist_WhenFilterRequestIsReceived_ThenReturnMatchingTodosAndStatusOk(GetTodosByFilterRequest filter, int expectedCount)
    {
        // Given
        List<Todo> testData = new(6);

        for (int i = 0; i < 5; i++)
        {
            var todo = new Todo($"task num {i + 1}");

            if (i % 2 == 0)
            {
                todo.AddBody($"body_{i + 1}");
            }
            testData.Add(todo);
        }

        var completedTodo = new Todo("This is done");
        completedTodo.Complete();
        testData.Add(completedTodo);

        _context.Todos.AddRange(testData);
        _context.SaveChanges();

        // When
        var result = await _client.PostAsync("/todos/filter", CreateRequestBody(filter));

        // Then
        Assert.True(result.IsSuccessStatusCode);
        var content = await result.Content.ReadFromJsonAsync<IEnumerable<Todo>>();

        Assert.NotNull(content);
        Assert.Equal(expectedCount, content.Count());

        _context.Todos.RemoveRange(testData);
        _context.SaveChanges();
    }

    public static IEnumerable<object[]> CreateFilterRequests()
    {
        var filter = new GetTodosByFilterRequest
        {
            Text = "task num 3"
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

        filter = new GetTodosByFilterRequest { Text = "body_" };
        expectedCount = 3;
        yield return new object[] { filter, expectedCount };
    }

    private static StringContent CreateRequestBody(object body)
    {
        return new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
    }
}