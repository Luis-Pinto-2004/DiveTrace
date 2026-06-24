using System.Text.Json.Serialization;
using DriveTraceCore.Api.Data;
using DriveTraceCore.Api.Middleware;
using DriveTraceCore.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Origens CORS configuráveis (Cors:AllowedOrigins) com defaults locais.
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://127.0.0.1:5173", "http://localhost:8088" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("DriveTraceFrontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Respostas de erro normalizadas (RFC 7807 / ProblemDetails).
builder.Services.AddProblemDetails();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddDbContext<DriveTraceDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<DemoEventService>();
builder.Services.AddScoped<OperationalEventService>();
builder.Services.AddScoped<ProductionFlowService>();
builder.Services.AddScoped<ProductionSimulationService>();
builder.Services.AddScoped<TraceGraphService>();
builder.Services.AddScoped<ReconditioningService>();
builder.Services.AddSingleton<PermissionCatalogService>();
builder.Services.AddHttpClient<IFiwareContextService, FiwareContextService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(12);
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DriveTrace Core API",
        Version = "v1",
        Description = "DRIVOLUTION WP3 — WIP Traceability and Monitoring Platform"
    });
});

var app = builder.Build();

// Tratamento centralizado de exceções -> ProblemDetails.
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSecurityHeaders();

using (var scope = app.Services.CreateScope())
{
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var db = scope.ServiceProvider.GetRequiredService<DriveTraceDbContext>();

    if (configuration.GetValue("Database:EnsureCreated", true))
    {
        await db.Database.EnsureCreatedAsync();
    }

    await SchemaEvolution.EnsurePhase34Async(db);

    if (configuration.GetValue("Database:SeedDemoData", true))
    {
        await DemoSeeder.SeedAsync(db);
    }
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "DriveTrace Core API v1");
    options.RoutePrefix = "swagger";
});

app.UseCors("DriveTraceFrontend");
app.UseRouting();
app.MapControllers();

// Endpoint de saúde para healthchecks do docker-compose e CI.
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "drivetrace-core-api" }));

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

// Necessário para os testes de integração (WebApplicationFactory<Program>).
public partial class Program { }
