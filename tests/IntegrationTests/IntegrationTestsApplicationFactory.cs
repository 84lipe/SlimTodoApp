using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using SlimTodoApp.Api.Data;
using Testcontainers.MsSql;

namespace SlimTodoApp.IntegrationTests;

public class IntegrationTestsApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
                                                       .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                                                       .WithPassword("tM9Z6F#KaT").Build();

    public Task InitializeAsync()
    {
        return _dbContainer.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services => {
            var dbContext = services.SingleOrDefault(x => x.ServiceType == typeof(DbContextOptions<TodoContext>));
            if (dbContext != null)
            {
                services.Remove(dbContext);
            }

            services.AddDbContext<TodoContext>(options => options
                                                          .UseSqlServer(_dbContainer.GetConnectionString()));

            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TodoContext>();
            context.Database.Migrate();
        });
    }

    public new Task DisposeAsync()
    {
        return _dbContainer.StopAsync();
    }
}   