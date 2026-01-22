using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rsl.Api.Extensions;
using Rsl.Api.Middleware;
using Rsl.Infrastructure;
using Rsl.Recommendation;
using Rsl.Llm;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Customize automatic 400 responses to include more details
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors.Select(err => new
                {
                    Field = e.Key,
                    Message = err.ErrorMessage
                }))
                .ToList();

            var result = new
            {
                Message = "Validation failed",
                Errors = errors
            };

            return new BadRequestObjectResult(result);
        };
    })
    .AddJsonOptions(options =>
    {
        // Configure JSON serialization
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();

// Configure custom services
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApiVersioningConfiguration();
builder.Services.AddOpenApiConfiguration();
builder.Services.AddCorsConfiguration(builder.Configuration);
builder.Services.AddRateLimitingConfiguration();
builder.Services.AddRegistrationSettings(builder.Configuration);

// Add Infrastructure layer (DbContext, repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// Add Recommendation Engine
builder.Services.AddRecommendationEngine();

// Add LLM services for ingestion
builder.Services.AddLlmServices(builder.Configuration);

// Add application services
builder.Services.AddScoped<Rsl.Api.Services.IAuthService, Rsl.Api.Services.AuthService>();
builder.Services.AddScoped<Rsl.Api.Services.IUserService, Rsl.Api.Services.UserService>();
builder.Services.AddScoped<Rsl.Api.Services.ISourceService, Rsl.Api.Services.SourceService>();
builder.Services.AddScoped<Rsl.Api.Services.IResourceService, Rsl.Api.Services.ResourceService>();
builder.Services.AddScoped<Rsl.Api.Services.IVoteService, Rsl.Api.Services.VoteService>();
builder.Services.AddScoped<Rsl.Api.Services.IRecommendationService, Rsl.Api.Services.RecommendationService>();
builder.Services.AddScoped<Rsl.Api.Services.IXAccountService, Rsl.Api.Services.XAccountService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<Rsl.Infrastructure.Data.RslDbContext>();

// Configure Problem Details
builder.Services.AddProblemDetails();

var app = builder.Build();

// Run database migrations on startup
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Production")
{
    using (var scope = app.Services.CreateScope())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<Rsl.Infrastructure.Data.RslDbContext>();

            // Check if database exists and create if needed
            var canConnect = await db.Database.CanConnectAsync();
            if (!canConnect)
            {
                logger.LogInformation("Database does not exist, creating...");
            }

            logger.LogInformation("Running database migrations...");
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrations completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database");
            throw;
        }
    }
}

// Configure the HTTP request pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("DefaultCorsPolicy");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();
