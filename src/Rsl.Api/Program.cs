using Rsl.Api.Extensions;
using Rsl.Api.Middleware;
using Rsl.Infrastructure;
using Rsl.Recommendation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure custom services
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApiVersioningConfiguration();
builder.Services.AddOpenApiConfiguration();
builder.Services.AddCorsConfiguration(builder.Configuration);
builder.Services.AddRateLimitingConfiguration();

// Add Infrastructure layer (DbContext, repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// Add Recommendation Engine
builder.Services.AddRecommendationEngine();

// Add application services
builder.Services.AddScoped<Rsl.Api.Services.IAuthService, Rsl.Api.Services.AuthService>();
builder.Services.AddScoped<Rsl.Api.Services.IUserService, Rsl.Api.Services.UserService>();
builder.Services.AddScoped<Rsl.Api.Services.ISourceService, Rsl.Api.Services.SourceService>();
builder.Services.AddScoped<Rsl.Api.Services.IResourceService, Rsl.Api.Services.ResourceService>();
builder.Services.AddScoped<Rsl.Api.Services.IVoteService, Rsl.Api.Services.VoteService>();
builder.Services.AddScoped<Rsl.Api.Services.IRecommendationService, Rsl.Api.Services.RecommendationService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<Rsl.Infrastructure.Data.RslDbContext>();

// Configure Problem Details
builder.Services.AddProblemDetails();

var app = builder.Build();

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
