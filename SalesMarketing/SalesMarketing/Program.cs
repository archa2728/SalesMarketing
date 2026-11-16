
using Microsoft.EntityFrameworkCore;
using SalesMarketing.Data;
using SalesMarketing.Repositories;
using SalesMarketing.Repositories.Interfaces;
using SalesMarketing.Services;
using SalesMarketing.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using SalesMarketing.Exceptions;
using Npgsql;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            return Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                   && uri.Host == "localhost";
        })
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

// Controllers
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token here."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// JWT configuration validation
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtExpiry = builder.Configuration.GetValue<int?>("Jwt:ExpiryInMinutes");

if (string.IsNullOrWhiteSpace(jwtKey) ||
    Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "JWT Key must be configured and at least 32 bytes long.");
}

if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    throw new InvalidOperationException("JWT Issuer is missing.");
}

if (string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException("JWT Audience is missing.");
}

if (jwtExpiry is null || jwtExpiry < 1 || jwtExpiry > 1440)
{
    throw new InvalidOperationException(
        "JWT ExpiryInMinutes must be between 1 and 1440.");
}

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey!))
    };
});

// Login Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("LoginRateLimit", httpContext =>
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?
            .ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ipAddress,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";

        await context.HttpContext.Response.WriteAsJsonAsync(
            new
            {
                message = "Too many login attempts. Please try again after one minute."
            },
            cancellationToken);
    };
});

// Database
builder.Services.AddDbContext<SalesMarketingDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Dependency Injection
builder.Services.AddScoped<ISalesmanRepository, SalesmanRepository>();
builder.Services.AddScoped<ISalesmanService, SalesmanService>();

builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

builder.Services.AddScoped<IMarketingTeamRepository, MarketingTeamRepository>();
builder.Services.AddScoped<IMarketingTeamService, MarketingTeamService>();

builder.Services.AddScoped<ISalesmanRemovalRequestRepository, SalesmanRemovalRequestRepository>();
builder.Services.AddScoped<ISalesmanRemovalRequestService, SalesmanRemovalRequestService>();

var app = builder.Build();

// Global Exception Handling
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features
            .Get<IExceptionHandlerFeature>()?.Error;

        var loggerFactory = context.RequestServices
            .GetRequiredService<ILoggerFactory>();

        var logger = loggerFactory.CreateLogger("GlobalExceptionHandler");

        int statusCode;
        string message;

        switch (exception)
        {
            case ConflictException:
                statusCode = StatusCodes.Status409Conflict;
                message = exception.Message;
                logger.LogWarning(exception, "Business conflict occurred.");
                break;

            case ArgumentException:
                statusCode = StatusCodes.Status400BadRequest;
                message = exception.Message;
                logger.LogWarning(exception, "Invalid request received.");
                break;

            case KeyNotFoundException:
                statusCode = StatusCodes.Status404NotFound;
                message = exception.Message;
                logger.LogWarning(exception, "Requested record was not found.");
                break;

            case UnauthorizedAccessException:
                statusCode = StatusCodes.Status401Unauthorized;
                message = exception.Message;
                logger.LogWarning(exception, "Unauthorized request.");
                break;

            case DbUpdateException dbException
                when dbException.GetBaseException() is PostgresException postgresException
                && postgresException.SqlState == "23505":

                statusCode = StatusCodes.Status409Conflict;
                message = "A record with the same unique value already exists.";
                logger.LogWarning(exception, "Database unique constraint violation.");
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                message = "An unexpected error occurred.";
                logger.LogError(exception, "An unexpected server error occurred.");
                break;
        }

        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(new
        {
            message
        });
    });
});

// PostgreSQL connection check
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<SalesMarketingDbContext>();

    var logger = scope.ServiceProvider
        .GetRequiredService<ILogger<Program>>();

    try
    {
        bool connected = dbContext.Database.CanConnect();

        if (connected)
        {
            logger.LogInformation(
                "PostgreSQL connection successful.");
        }
        else
        {
            logger.LogError(
                "Could not connect to PostgreSQL.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(
            ex,
            "Database connection check failed.");
    }
}

// HTTP Request Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("AllowAngular");

// Rate limiter must run after routing so it can read endpoint metadata.
app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
