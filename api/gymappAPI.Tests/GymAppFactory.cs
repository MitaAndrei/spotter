using gymappAPI.Data;
using gymappAPI.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace gymappAPI.Tests;

/// <summary>
/// Runs the real API in memory, with an in-memory SQLite database instead of Postgres
/// and a fake S3, so the tests need no external services.
/// </summary>
public class GymAppFactory : WebApplicationFactory<Program>
{
    // An in-memory SQLite database lives as long as its connection stays open.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public FakeS3Service S3 { get; } = new();

    // Tests share one client address and log in constantly, so the login limit is off unless a test wants it.
    protected virtual int AuthPermitLimit => 1_000_000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not "Development", so the developer's User Secrets aren't loaded.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:AppDbConnectionString", "Host=unused-in-tests");
        builder.UseSetting("JwtSettings:Key", "integration-tests-signing-key-that-is-long-enough-for-hs256");
        builder.UseSetting("RateLimiting:AuthPermitLimit", AuthPermitLimit.ToString());

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions>();
            // EF Core 9+ keeps the UseNpgsql setup from Program.cs here, separate from the options.
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            _connection.Open();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<IS3Service>();
            services.AddSingleton<IS3Service>(S3);
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        EnsureDatabaseCreated();
    }

    private bool _databaseCreated;

    private void EnsureDatabaseCreated()
    {
        if (_databaseCreated) return;
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        _databaseCreated = true;
    }

    public async Task WithDbAsync(Func<AppDbContext, Task> action)
    {
        EnsureDatabaseCreated();
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}

public class FakeS3Service : IS3Service
{
    public List<string> DeletedKeys { get; } = [];

    public Task<string> UploadFileAsync(IFormFile file, string? fileName = null) =>
        Task.FromResult(fileName ?? $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");

    public Task<bool> DeleteFileAsync(string fileKey)
    {
        DeletedKeys.Add(fileKey);
        return Task.FromResult(true);
    }

    public Task<string> GetPreSignedUrlAsync(string fileKey, int expirationMinutes = 60) =>
        Task.FromResult($"https://s3.test/{fileKey}");
}
