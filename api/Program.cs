using System.Text.Json.Serialization;
using DriveTraceCore.Api.Data;
using DriveTraceCore.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("DriveTraceFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173", "http://localhost:8088")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

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
builder.Services.AddScoped<ProductionFlowService>();
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
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
