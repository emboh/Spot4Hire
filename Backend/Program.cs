using FluentValidation;
using Gridify;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Http.Resilience;
using Scalar.AspNetCore;
using Spot4Hire.Backend.Common;
using Spot4Hire.Backend.Data;
using Spot4Hire.Backend.Data.Entities;
using Spot4Hire.Backend.Filters;
using Spot4Hire.Backend.OpenApi;
using Spot4Hire.Backend.Services;
using Spot4Hire.Backend.Services.Abstractions;
using Spot4Hire.Backend.Services.Assistant;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Add services to the container.
builder.AddSqlServerDbContext<AppDbContext>(
    "spot4hiredb",
    configureDbContextOptions: options => options.UseSqlServer(sql => sql.UseNetTopologySuite()));
builder.AddRedisDistributedCache("redis");

builder.Services.AddControllers(options =>
    {
        options.Filters.Add<FluentValidationFilter>();
        // Let FluentValidation own request validation instead of the implicit
        // "non-nullable reference type is required" model-binding rule.
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Gridify: only whitelisted (mapped) fields are usable; ignore anything else.
GridifyGlobalConfiguration.IgnoreNotMappedFields = true;

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GridifyExceptionHandler>();

builder.Services.AddScoped<IVenueService, VenueService>();
builder.Services.AddScoped<IUnitService, UnitService>();
builder.Services.AddScoped<IOpeningHourService, OpeningHourService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IUserService, UserService>();

// AI assistant. All scoped: the tools use the scoped services above, and the
// context holds per-request facts (like the user's location).
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<AssistantContext>();
builder.Services.AddScoped<AssistantTools>();
builder.Services.AddScoped<IAssistantService, AssistantService>();

// Rate limiting: 100 requests/minute per authenticated user, or per client IP
// for anonymous callers. This limiter is in-memory (per app instance).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var partitionKey =
            context.User.Identity?.Name
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
        });
    });

    // Applies on top of the global limiter, on endpoints marked
    // [EnableRateLimiting(RateLimitPolicies.Assistant)].
    options.AddPolicy(RateLimitPolicies.Assistant, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.Identity?.Name
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
            }));
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(o =>
{
    o.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    o.AddOperationTransformer<GridifyParameterTransformer>();
});

builder.Services
    .AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>();

// ServiceDefaults adds the standard resilience handler to every HttpClient:
// 10 s per attempt, 30 s total, and retries (even for POST). That is too short
// for a local LLM, and a retry would send the same prompt to the model again.
// The only outgoing HTTP from this service is the model, so relax it for all
// clients here.
builder.Services.ConfigureAll<HttpStandardResilienceOptions>(options =>
{
    options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(3);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);

    // Must be at least twice the attempt timeout (checked at startup).
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(6);

    // Never retry POST: a model call is slow and not safe to repeat.
    options.Retry.DisableForUnsafeHttpMethods();
});

builder.AddOllamaApiClient("chat")
    .AddChatClient()
    // Stops a confused model from calling tools in an endless loop.
    .UseFunctionInvocation(configure: f => f.MaximumIterationsPerRequest = 8)
    .UseOpenTelemetry(configure: c =>
        c.EnableSensitiveData = builder.Environment.IsDevelopment());

var app = builder.Build();

app.UseExceptionHandler();

app.MapDefaultEndpoints();

app.MapIdentityApi<ApplicationUser>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseDeveloperExceptionPage();

    app.MapScalarApiReference();

    app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    await RoleSeeder.SeedRolesAsync(scope.ServiceProvider);

    if (app.Environment.IsDevelopment())
    {
        await DataSeeder.SeedAsync(scope.ServiceProvider);
    }
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

app.UseCors();

app.Run();

public partial class Program { }