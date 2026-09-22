using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UniPilot.Application.Activities;
using UniPilot.Application.Auth;
using UniPilot.Application.Courses;
using UniPilot.Application.Documents;
using UniPilot.Application.Notifications;
using UniPilot.Application.Projects;
using UniPilot.Application.Requirements;
using UniPilot.Infrastructure.Activities;
using UniPilot.Infrastructure.Auth;
using UniPilot.Infrastructure.Courses;
using UniPilot.Infrastructure.Documents;
using UniPilot.Infrastructure.Notifications;
using UniPilot.Infrastructure.Persistence;
using UniPilot.Infrastructure.Projects;
using UniPilot.Infrastructure.Requirements;

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
        services.AddScoped<IAcademicProjectService,
            AcademicProjectService>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<IProjectDocumentService,
            ProjectDocumentService>();
        services.AddSingleton<IPdfTextExtractor,
            PdfPigTextExtractor>();
        services.AddHttpClient<IRequirementExtractor,
            GeminiRequirementExtractor>();
        services.AddScoped<IProjectRequirementService,
            ProjectRequirementService>();
        services.AddScoped<INotificationService,
            NotificationService>();
        services.AddScoped<IProjectActivityService,
            ProjectActivityService>();

        return services;
    }
}
