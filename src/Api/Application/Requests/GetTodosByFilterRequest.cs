namespace SlimTodoApp.Api.Application.Requests;

public sealed class GetTodosByFilterRequest
{
    public string? Title { get; set; } = string.Empty;
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public bool? Completed { get; set; }
}