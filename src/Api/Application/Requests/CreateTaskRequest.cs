namespace SlimTodoApp.Api.Application.Requests;

public sealed record CreateTaskRequest(string Title, string Body = "");