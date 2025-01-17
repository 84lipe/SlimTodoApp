using Microsoft.EntityFrameworkCore;
using SlimTodoApp.Api.Application.Requests;
using SlimTodoApp.Api.Data;
using SlimTodoApp.Api.Domain.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EntityFramework
builder.Services.AddDbContext<TodoContext>(options => 
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/todos", (TodoContext context) => 
{
    var result = context.Todos.ToList();

    if (result is not { Count: > 0})
    {
        return Enumerable.Empty<Todo>();
    }

    return result;
})
.WithName("GetAllTodos")
.WithOpenApi();

app.MapPost("/todos/filter", (TodoContext context, GetTodosByFilterRequest filter) => 
{
    var query = context.Todos.AsNoTracking();

    if (!string.IsNullOrEmpty(filter.Text))
    {
        query = query.Where(t => t.Title.Contains(filter.Text) ||
        (t.Body != null && t.Body.Contains(filter.Text)));
    }

    if (filter.DateFrom is not null)
    {
        query = query.Where(t => t.Created >= filter.DateFrom);
    }

    if (filter.DateTo is not null)
    {
        query = query.Where(t => t.Created <= filter.DateTo);
    }

    if (filter.Completed is not null)
    {
        query = query.Where(t => t.Completed == filter.Completed);
    }

    var result = query.ToList();

    if (result is not { Count: > 0})
    {
        return Enumerable.Empty<Todo>();
    }

    return result;
})
.WithName("GetTodosByFilter")
.WithOpenApi();

app.MapGet("/todos/{id}", (TodoContext context, Guid id) => 
{
    var result = context.Todos.FirstOrDefault(x => x.Id == id);

    if (result is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(result);
})
.WithName("GetTodoById")
.WithOpenApi();

app.MapPost("/todos", (TodoContext context, CreateTaskRequest request) => 
{
    var todo = new Todo(request.Title);
    todo.AddBody(request.Body);

    context.Todos.Add(todo);

    context.SaveChanges();

    return Results.Created($"/todos/{todo.Id}", todo);
})
.WithName("CreateTodo")
.WithOpenApi();

app.MapPatch("/todos/{id}/complete", (TodoContext context, Guid id) => 
{
    var todo = context.Todos.FirstOrDefault(x => x.Id == id);

    if (todo is null)
    {
        return Results.BadRequest("Task Id is invalid.");
    }

    todo.Complete();

    context.SaveChanges();

    return Results.NoContent();
})
.WithName("CompleteTask")
.WithOpenApi();

app.Run();
public partial class Program { }