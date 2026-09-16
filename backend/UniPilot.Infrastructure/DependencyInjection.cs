using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UniPilot.Infrastructure.Persistence;
using UniPilot.Application.Auth;
using UniPilot.Infrastructure.Auth;
using UniPilot.Application.Courses;
using UniPilot.Infrastructure.Courses;
using UniPilot.Application.Projects;
using UniPilot.Infrastructure.Projects;
using UniPilot.Application.Documents;
using UniPilot.Infrastructure.Documents;
namespace UniPilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection was not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IAcademicProjectService,AcademicProjectService>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<IProjectDocumentService,ProjectDocumentService>();
        services.AddSingleton<IPdfTextExtractor,PdfPigTextExtractor>();
        return services;
    }
}