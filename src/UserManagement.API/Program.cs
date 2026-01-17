using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using FluentValidation;
using FluentValidation.AspNetCore;
using UserManagement.API.Middleware;
using UserManagement.Repository;
using UserManagement.Services;
using UserManagement.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

// ==================== Configure Logging (Serilog) ====================
builder.Host.UseSerilog((context, services, config) =>
{
    config
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            path: Path.Combine(AppContext.BaseDirectory, "logs/usermanagement-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
});

// ==================== Add Services to Container ====================
// Controllers
builder.Services.AddControllers();

// API Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Validation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<UserManagement.API.Validators.RegisterUserRequestValidator>();
// Validators will be auto-discovered through assembly scanning

// ==================== Configure JWT Authentication ====================
// 1. Bind JwtSettings from configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(nameof(JwtSettings)));

// 2. Get JWT settings to configure authentication
var jwtSettings = builder.Configuration.GetSection(nameof(JwtSettings)).Get<JwtSettings>();
if (jwtSettings == null)
    throw new InvalidOperationException("JwtSettings not configured in appsettings.json");

var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

// 3. Configure Authentication Handler (JWT Bearer)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero // No clock skew for production
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT authentication failed: {Exception}", context.Exception?.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var user = context.Principal?.Identity?.Name;
            logger.LogInformation("JWT token validated for user: {User}", user);
            return Task.CompletedTask;
        }
    };
});

// 4. Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("UserOrAdmin", policy =>
        policy.RequireRole("User", "Admin"));
});

// Layer Registration (Dependency Injection)
// Order: Repository -> Services -> No more dependencies
builder.Services.AddRepositoryLayer(builder.Configuration);
builder.Services.AddServiceLayer(builder.Configuration);

// ==================== Build Application ====================
var app = builder.Build();

// ==================== Configure HTTP Request Pipeline ====================
// Exception handling middleware (must be early in the pipeline)
app.UseMiddleware<GlobalExceptionMiddleware>();

// Generate OpenAPI/Swagger documentation
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Management API v1");
        c.RoutePrefix = string.Empty;
    });
}

// Security
app.UseHttpsRedirection();

// Authentication & Authorization (order matters!)
app.UseAuthentication();  // Must come before UseAuthorization
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Log application startup
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application starting - Environment: {Environment}", app.Environment.EnvironmentName);

// Run application
app.Run();

logger.LogInformation("Application shutting down");
