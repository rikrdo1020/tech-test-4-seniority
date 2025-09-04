using api.Data;
using api.Data.Repositories.Implementations;
using api.Data.Repositories.Interfaces;
using api.Helpers.Middlewares;
using api.Services;
using api.Services.Implementations;
using api.Services.Interfaces;
using api.Services.Publishers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddLogging();

builder.UseMiddleware<GlobalExceptionMiddleware>();

var enableJwt = Environment.GetEnvironmentVariable("EnableJwtMiddleware");

if (string.Equals(enableJwt, "true", StringComparison.OrdinalIgnoreCase))
{
    builder.UseMiddleware<JwtValidationMiddleware>();
}

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddDbContext<DataContext>(option => 
    option.UseSqlServer(Environment.GetEnvironmentVariable("SqlConnectionString")));

builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IGraphService, GraphService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationFactory, NotificationFactory>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services.AddSingleton<INotificationPublisher, NoOpNotificationPublisher>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
    dataContext.Database.Migrate();
}
app.Run();