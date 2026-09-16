using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UniPilot.Infrastructure.Persistence;
using UniPilot.Application.Documents;

namespace UniPilot.Tests.Infrastructure;

public sealed class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private readonly string _databaseName =
        $"UniPilotTests-{Guid.NewGuid()}";

    public CustomWebApplicationFactory()
    {
        var jwtKey = Convert.ToBase64String(
            Enumerable.Range(1, 64)
                .Select(number => (byte)number)
                .ToArray());

        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Host=localhost;Database=unused;Username=unused;Password=unused");

        Environment.SetEnvironmentVariable(
            "Jwt__Key",
            jwtKey);

        Environment.SetEnvironmentVariable(
            "Jwt__Issuer",
            "UniPilot.Tests");

        Environment.SetEnvironmentVariable(
            "Jwt__Audience",
            "UniPilot.Tests.Client");
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<
                IDbContextOptionsConfiguration<AppDbContext>>();

            services.RemoveAll<
                DbContextOptions<AppDbContext>>();

            services.RemoveAll<AppDbContext>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
           
            services.RemoveAll<IFileStorage>();

            services.AddSingleton<IFileStorage, TestFileStorage>();
        });
    }
}