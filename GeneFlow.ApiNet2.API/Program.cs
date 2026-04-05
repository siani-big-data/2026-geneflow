using GeneFlow.ApiNet2.API.Endpoints;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add API services (includes Infrastructure, MediatR, JWT, Swagger)
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

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
