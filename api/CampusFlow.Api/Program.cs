using CampusFlow.Api.Auth;
using CampusFlow.Api.Data;
using CampusFlow.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

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

/*
 * Supabase already contains internal system tables. EF Core's standard
 * EnsureCreatedAsync method can therefore assume that the database has
 * already been initialized even when the CampusFlow tables do not exist.
 *
 * For PostgreSQL, check specifically for the CampusFlow Users table.
 * If it is absent, create every table defined in CampusFlowDbContext.
 */
await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider
        .GetRequiredService<CampusFlowDbContext>();

    if (database.Database.IsNpgsql())
    {
        await database.Database.OpenConnectionAsync();

        try
        {
            await using var command =
                database.Database.GetDbConnection().CreateCommand();

            command.CommandText =
                """SELECT to_regclass('public."Users"') IS NOT NULL;""";

            var result = await command.ExecuteScalarAsync();
            var campusFlowTablesExist =
                result is bool exists && exists;

            if (!campusFlowTablesExist)
            {
                var databaseCreator = database
                    .GetService<IRelationalDatabaseCreator>();

                await databaseCreator.CreateTablesAsync();
            }
        }
        finally
        {
            await database.Database.CloseConnectionAsync();
        }
    }
    else
    {
        // SQLite is used only for local development.
        await database.Database.EnsureCreatedAsync();
    }
}

app.Run();

public partial class Program
{
}