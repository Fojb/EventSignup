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

app.UseAuthorization();

app.MapControllers();

app.Run();
