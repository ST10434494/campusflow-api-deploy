using CampusFlow.Api.Auth;
using CampusFlow.Api.Data;
using CampusFlow.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

var postgresConnection =
    builder.Configuration.GetConnectionString("Postgres");

var localSqliteConnection =
    builder.Configuration.GetConnectionString("LocalSqlite")
    ?? "Data Source=campusflow.db";

builder.Services.AddDbContext<CampusFlowDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(postgresConnection))
    {
        // Render uses the Supabase PostgreSQL connection.
        options.UseNpgsql(
            postgresConnection,
            postgres => postgres.EnableRetryOnFailure());
    }
    else
    {
        // Local development fallback.
        options.UseSqlite(localSqliteConnection);
    }
});

builder.Services.AddSingleton<FirebaseTokenVerifier>();
builder.Services.AddScoped<UserProvisioningService>();

builder.Services
    .AddAuthentication(FirebaseAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, FirebaseAuthenticationHandler>(
        FirebaseAuthenticationHandler.SchemeName,
        _ => { });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "CampusFlow API",
    timestampUtc = DateTime.UtcNow
}));

app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider
        .GetRequiredService<CampusFlowDbContext>();

    await database.Database.EnsureCreatedAsync();
}

app.Run();

public partial class Program
{
}