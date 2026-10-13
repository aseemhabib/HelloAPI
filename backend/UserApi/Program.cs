using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<IUserRepository, UserRepository>();
//register the password hasher service
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "lab.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;

        // We're currently developing over HTTP locally.
        // Change this to Always when you move to HTTPS.
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;

        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "AdminOnly",
        policy =>
        {
            policy.RequireRole("Admin");
        });

    options.AddPolicy(
        "SupportOrAdmin",
        policy =>
        {
            policy.RequireRole(
                "Support",
                "Admin");
        });
});

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/var/lib/aspnet-dpkeys"))
    .SetApplicationName("UserApi");

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

var redisHost =
    builder.Configuration["REDIS_HOST"]
    ?? throw new InvalidOperationException(
        "REDIS_HOST is missing.");

var redisPort =
    builder.Configuration["REDIS_PORT"]
    ?? throw new InvalidOperationException(
        "REDIS_PORT is missing.");

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration =
        $"{redisHost}:{redisPort}";

    options.InstanceName =
        "LoadBalancerLab:";
});


var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
//this must be called after UseAuthentication and UseAuthorization, but before any endpoints that require antiforgery validation
app.UseAntiforgery();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health/live", () =>
{
    return Results.Ok("Alive");
});

app.MapGet("/health/ready", () =>
{
    return Results.Ok("Ready");
});

app.MapGet("/api/user", () =>
{
    var instanceName = Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? "local";
    var guid = Guid.NewGuid();


    return Results.Ok(new { message = $"Hello from {instanceName}!", instance = instanceName, machinename = Environment.MachineName, timestamputc = DateTime.UtcNow, userid = guid });
});

app.MapPost("/api/users",
    async (
        //supplied by client
        CreateUserRequest request,
        //supplied by DI
        IUserRepository repository,
        //supplied by framework
        IPasswordHasher<User> passwordHasher,
        CancellationToken cancellationToken) =>
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            CreatedUtc = DateTime.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        await repository.CreateAsync(
            user,
            cancellationToken);

        return Results.Created(
            $"/api/users/{user.Id}",
            user);
    });

app.MapPost("/api/auth/login",
    async (
        //supplied by client
        LoginRequest request,
        //supplied by DI
        IUserRepository repository,
        //supplied by framework
        IPasswordHasher<User> passwordHasher,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
    {
        var user = await repository.GetByEmailAsync(
            request.Email,
            cancellationToken);

        if (user is null)
        {
            return Results.Unauthorized();
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return Results.Unauthorized();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
        };
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);

        return Results.Ok(new LoginResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email));
    });

    app.MapGet("/api/auth/me",
    (ClaimsPrincipal user) =>
    {
        var roles = user
            .FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray();

        return Results.Ok(new
        {
            Id = user.FindFirstValue(
                ClaimTypes.NameIdentifier),

            Email = user.FindFirstValue(
                ClaimTypes.Email),

            Name = user.FindFirstValue(
                ClaimTypes.Name),

            Roles = roles
        });
    })
    .RequireAuthorization();

    app.MapPost("/api/auth/logout",
    async (HttpContext httpContext,
           IAntiforgery antiforgery) =>
    {
        await antiforgery.ValidateRequestAsync(httpContext);
        await httpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return Results.Ok();
    })
    .RequireAuthorization();

    app.MapGet("/api/auth/csrf-token",
    (HttpContext context, IAntiforgery antiforgery) =>
    {
        var tokens = antiforgery.GetAndStoreTokens(context);

        return Results.Ok(new
        {
            token = tokens.RequestToken
        });
    })
    .RequireAuthorization();

    app.MapGet("/api/admin/test", () =>
    {
        return Results.Ok(
            new
            {
                message = "Admin access granted."
            });
    })
    .RequireAuthorization("AdminOnly");

    app.MapGet("/api/support/test", () =>
    {
        return Results.Ok(
            new
            {
                message = "Support access granted."
            });
    })
    .RequireAuthorization("SupportOrAdmin");

    app.MapGet("/api/cpu-test", () =>
    {
        var end = DateTime.UtcNow.AddMilliseconds(500);

        while (DateTime.UtcNow < end)
        {
            Math.Sqrt(Random.Shared.NextDouble());
        }

        return Results.Ok(new
        {
            message = "CPU work completed",
            machinename = Environment.MachineName
        });
    });

    app.MapGet(
    "/api/dashboard/summary",
    async (
        IUserRepository repository,
        IDistributedCache cache,
        ILogger<Program> logger,
        CancellationToken cancellationToken) =>
    {

        logger.LogInformation("Cache Implementation: {CacheType}", cache.GetType().FullName);

        const string cacheKey = "dashboard:summary";

        var cachedJson =
            await cache.GetStringAsync(
                cacheKey,
                cancellationToken);

        if (cachedJson is not null)
        {
            logger.LogInformation(
                "Dashboard cache HIT");

            var cachedSummary =
                JsonSerializer.Deserialize<DashboardSummary>(
                    cachedJson);

            return Results.Ok(cachedSummary);
        }

        logger.LogInformation(
            "Dashboard cache MISS");

        var summary =
            await repository.GetDashboardSummaryAsync(
                cancellationToken);

        var json =
            JsonSerializer.Serialize(summary);

        await cache.SetStringAsync(
            cacheKey,
            json,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow =
                    TimeSpan.FromSeconds(30)
            },
            cancellationToken);

        return Results.Ok(summary);
    })
    .RequireAuthorization();


// app.UseHttpsRedirection();

// var summaries = new[]
// {
//     "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
// };

// app.MapGet("/weatherforecast", () =>
// {
//     var forecast =  Enumerable.Range(1, 5).Select(index =>
//         new WeatherForecast
//         (
//             DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//             Random.Shared.Next(-20, 55),
//             summaries[Random.Shared.Next(summaries.Length)]
//         ))
//         .ToArray();
//     return forecast;
// })
// .WithName("GetWeatherForecast");

app.Run();

// record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
// {
//     public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
// }

public sealed record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password);

public sealed record LoginRequest(
    string Email,
    string Password);

public sealed record LoginResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email);

public sealed record DashboardSummary(
    int TotalUsers,
    int UsersLastSevenDays,
    int AdminUsers,
    int NormalUsers);