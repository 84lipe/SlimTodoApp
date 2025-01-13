namespace SlimTodoApp.Api.Domain.Models;

public sealed class Todo
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string? Body { get; private set; }
    public bool Completed { get; set; }
    public DateTime Created { get; private set; }
    public DateTime Updated { get; private set; }

    public Todo(string title)
    {
        Title = title;
        Created = DateTime.UtcNow;
    }

    public void Complete() => SetCompleted(true);
    public void Open() => SetCompleted(false);

    public void AddBody(string body)
    {
        Body = body;
    }

    private void SetCompleted(bool isCompleted)
    {
        Completed = isCompleted;
        Updated = DateTime.UtcNow;
    }
}