namespace SlimTodoApp.Api.Application.Requests;

public sealed class CreateTaskRequest
{
    public string Title { get; set; } = string.Empty;
}