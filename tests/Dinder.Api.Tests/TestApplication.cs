using Dinder.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Dinder.Api.Tests;

public sealed class TestApplication : WebApplicationFactory<Program>
{
    public const string Password = "Test-only-Passphrase42!";
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        connection.Open();
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        { ["Bootstrap:ChefPassword"] = Password, ["Bootstrap:MemberPassword"] = Password }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DinderDbContext>();
            services.RemoveAll<DbContextOptions<DinderDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<DinderDbContext>>();
            services.AddDbContext<DinderDbContext>(options => options.UseSqlite(connection));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<DinderDbContext>().Database.EnsureCreated();
        DevelopmentSeed.Run(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<IConfiguration>()).GetAwaiter().GetResult();
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) connection.Dispose();
    }
}
