using System.Threading.RateLimiting;
using EventSignup.Api.Controllers;
using EventSignup.Api.Search;
using EventSignup.Api.Services;
using EventSignup.Data;
using EventSignup.Data.Seeding;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

var eventTimeZone = EventTimeZone.FromId(builder.Configuration["EventTimeZone"]
    ?? throw new InvalidOperationException("'EventTimeZone' is not configured."));
builder.Services.AddSingleton(eventTimeZone);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
        .UseSeeding((context, _) => DbSeeder.Seed(context, eventTimeZone))
        .UseAsyncSeeding((context, _, cancellationToken) => DbSeeder.SeedAsync(context, eventTimeZone, cancellationToken));

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<RegistrationService>();

var llmSection = builder.Configuration.GetSection(LlmOptions.SectionName);
var llmOptions = llmSection.Get<LlmOptions>() ?? new LlmOptions();
builder.Services.Configure<LlmOptions>(llmSection);
builder.Services.AddHttpClient<LlmQueryParser>(client =>
{
    client.BaseAddress = new Uri(llmOptions.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(llmOptions.TimeoutSeconds);
});
builder.Services.AddMemoryCache(options => options.SizeLimit = 1000);
builder.Services.AddScoped<EventSearchService>();

// Every search can cost an LLM call, so limit it per client IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(EventSearchController.RateLimitPolicy, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = llmOptions.RequestsPerMinutePerClient,
                Window = TimeSpan.FromMinutes(1),
            }));
    options.OnRejected = (context, cancellationToken) =>
        context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many requests",
                Detail = "Preveč iskanj v kratkem času. Poskusite znova čez minuto.",
            },
        });
});

// The Blazor UI is served from a different origin (host project), so the browser needs CORS to call the API.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Single-instance demo app: apply pending migrations (and run the seeder) on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

app.Run();
