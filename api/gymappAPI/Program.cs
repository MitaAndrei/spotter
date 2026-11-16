using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using gymappAPI.Configuration;
using gymappAPI.Controllers;
using gymappAPI.Data;
using gymappAPI.Errors;
using gymappAPI.Hubs;
using gymappAPI.Managers;
using gymappAPI.Models;
using gymappAPI.Services;
using Microsoft.OpenApi.Models;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHttpContextAccessor();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Add support for file uploads
    options.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });
    
});
builder.Services.AddSignalR();


builder.Services.AddSingleton<IS3Service, S3Service>();
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddScoped<IAuthManager, AuthManager>();
builder.Services.AddScoped<IUserManager, UserManager>();
builder.Services.AddScoped<IWorkoutManager, WorkoutManager>();
builder.Services.AddScoped<IFoodManager, FoodManager>();
builder.Services.AddScoped<IFriendsManager, FriendsManager>();
builder.Services.AddScoped<IChatManager, ChatManager>();



// Secrets come from User Secrets (Development) or environment variables, never from appsettings.json.
var connectionString = builder.Configuration.GetConnectionString("AppDbConnectionString");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "Missing ConnectionStrings:AppDbConnectionString. Set it with 'dotnet user-secrets' or the ConnectionStrings__AppDbConnectionString environment variable.");

var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
var jwtSettings = jwtSection.Get<JwtSettings>() ?? new JwtSettings();
if (string.IsNullOrWhiteSpace(jwtSettings.Key))
    throw new InvalidOperationException(
        "Missing JwtSettings:Key. Set it with 'dotnet user-secrets' or the JwtSettings__Key environment variable.");

builder.Services.Configure<JwtSettings>(jwtSection);
builder.Services.Configure<AwsSettings>(builder.Configuration.GetSection(AwsSettings.SectionName));

// Kerberos isn't used. Without this, Npgsql tries to load the Kerberos library on each start,
// which the container image doesn't have.
var npgsqlConnectionString = new NpgsqlConnectionStringBuilder(connectionString)
{
    GssEncryptionMode = GssEncryptionMode.Disable
}.ConnectionString;
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(npgsqlConnectionString));

// Redis when configured, otherwise an in-memory cache so the app still runs without Redis.
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "gymapp:";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

// The client's address, e.g. Cors__AllowedOrigins__0=https://gymapp.example.com in production.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Client", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
    };
    
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Limits password guessing and mass sign-ups, per client address. Behind a reverse proxy, this also needs
// ASPNETCORE_FORWARDEDHEADERS_ENABLED=true; otherwise all users share the proxy's address.
var authPermitLimit = builder.Configuration.GetValue("RateLimiting:AuthPermitLimit", 10);
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(AuthController.AuthRateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = authPermitLimit, Window = TimeSpan.FromMinutes(1) }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Too many attempts. Please wait a minute and try again." }, cancellationToken);
    };
});

var app = builder.Build();

// Docker turns this on, so that the app can start on an empty database.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

// The live demo sets this, to create the "demo" account with sample data on first start.
var demoPassword = app.Configuration["Demo:Password"];
if (!string.IsNullOrWhiteSpace(demoPassword))
{
    using var scope = app.Services.CreateScope();
    await DemoSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(), demoPassword);
}

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Client");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chatHub");


app.Run();

// Lets the integration tests start the app with WebApplicationFactory<Program>.
public partial class Program { }
