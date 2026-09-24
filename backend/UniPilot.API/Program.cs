using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UniPilot.API.Authentication;
using UniPilot.Application.Auth;
using UniPilot.Application.Courses;
using UniPilot.Application.ProjectMembers;
using UniPilot.Application.Tasks;
using UniPilot.Application.Users;
using UniPilot.Infrastructure;
using UniPilot.Infrastructure.Auth;
using UniPilot.Infrastructure.Courses;
using UniPilot.Infrastructure.Persistence;
using UniPilot.Infrastructure.ProjectMembers;
using UniPilot.Infrastructure.Tasks;
using UniPilot.Infrastructure.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var allowedOrigins = builder.Configuration
    .GetSection("Frontend:AllowedOrigins")
    .Get<string[]>()
    ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "Frontend",
        policy =>
        {
            policy
                .SetIsOriginAllowed(origin =>
                {
                    if (!Uri.TryCreate(
                            origin,
                            UriKind.Absolute,
                            out var uri))
                    {
                        return false;
                    }

                    return uri.IsLoopback ||
                        allowedOrigins.Contains(
                            origin.TrimEnd('/'),
                            StringComparer.OrdinalIgnoreCase);
                })
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services.AddInfrastructure(
    builder.Configuration);

builder.Services.AddScoped<
    ICourseService,
    CourseService>();

builder.Services.AddScoped<
    IProjectTaskService,
    ProjectTaskService>();

builder.Services.AddScoped<
    IUserAccountService,
    UserAccountService>();

var jwtKey =
    builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT key was not configured.");

var jwtIssuer =
    builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "JWT issuer was not configured.");

var jwtAudience =
    builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "JWT audience was not configured.");

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Convert.FromBase64String(
                            jwtKey)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
    });

builder.Services.AddSingleton<
    ITokenService,
    JwtTokenService>();

builder.Services.AddAuthorization();
builder.Services.AddOpenApi();

builder.Services.AddHttpClient<
    IPasswordResetEmailSender,
    ResendPasswordResetEmailSender>();

builder.Services.AddHttpClient<
    IProjectInvitationEmailSender,
    ResendProjectInvitationEmailSender>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsEnvironment(
        "Testing"))
{
    await using var scope =
        app.Services.CreateAsyncScope();

    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<
                AppDbContext>();

    await dbContext.Database
        .MigrateAsync();
}

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapGet(
        "/health",
        () => Results.Ok(new
        {
            status = "healthy"
        }))
    .AllowAnonymous();

app.MapControllers();

app.Run();

public partial class Program
{
}