using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using ProjectCalculationService.Api.Configuration;
using ProjectCalculationService.Api.Filters;
using ProjectCalculationService.Application.Interfaces;
using ProjectCalculationService.Application.Services;
using ProjectCalculationService.DataAccess;
using ProjectCalculationService.DataAccess.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureSerilog();

builder.Services
    .AddControllers(options => options.Filters.Add<CustomExceptionFilter>())
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ProjectCalculationService API", Version = "v1" });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ProjectCalculationDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IProjectStepAnswerRepository, ProjectStepAnswerRepository>();
builder.Services.AddScoped<ISharingRepository, SharingRepository>();
builder.Services.AddScoped<IViewerCommentRepository, ViewerCommentRepository>();
builder.Services.AddScoped<IProjectCalculationApplicationService, ProjectCalculationApplicationService>();
builder.Services.AddScoped<IProjectReportService, ProjectReportService>();

builder.Services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConsole();
    config.AddDebug();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ProjectCalculationDbContext>();
    dbContext.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.MapControllers();
app.Run();

