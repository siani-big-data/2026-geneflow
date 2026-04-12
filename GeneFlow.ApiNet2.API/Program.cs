using GeneFlow.ApiNet2.API.Endpoints;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.API.Middleware;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Add API services (includes Infrastructure, MediatR, JWT, Swagger)
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

// Initialize database (apply migrations and seed data)
await app.InitializeDatabaseAsync();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "GeneFlow API v1");
    });
}

app.UseCorrelationId();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors();

// Serve static files from storage folder (profile photos, etc.)
var storagePath = builder.Configuration.GetValue<string>("Storage:BasePath") ?? "./uploads";
var storageFullPath = Path.GetFullPath(storagePath);
if (!Directory.Exists(storageFullPath))
{
    Directory.CreateDirectory(storageFullPath);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(storageFullPath),
    RequestPath = "/storage"
});

app.UseAuthentication();
app.UseAuthorization();

// Map all endpoints from IEndpoint implementations
app.MapEndpoints();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithTags("Health")
    .ExcludeFromDescription();

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
