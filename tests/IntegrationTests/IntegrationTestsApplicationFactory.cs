using System.Data.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SlimTodoApp.Api.Data;

namespace SlimTodoApp.IntegrationTests;

public class IntegrationTestsApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string connection = "Server=[::1];Database=TestDb;User Id=sa;Password=Nethraal@2024;trusted_connection=false;Encrypt=false";

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services => {
            var dbContext = services.SingleOrDefault(x => x.ServiceType == typeof(DbContextOptions<TodoContext>));
            if (dbContext != null)
            {
                services.Remove(dbContext);
            }
            
            services.AddDbContext<TodoContext>(options => options.UseSqlServer(connection));

            var provider = services.BuildServiceProvider();

            // using var scope = provider.CreateScope();
            // var context = scope.ServiceProvider.GetRequiredService<TodoContext>();
            
            //context.Database.EnsureCreated();
            var context = provider.GetRequiredService<TodoContext>();
            context.Database.Migrate();
        });

        builder.UseEnvironment("Development");

        return base.CreateHost(builder);
    }
}   