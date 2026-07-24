using EmployeeMonitoringAPI.Configuration;
using EmployeeMonitoringAPI.Interfaces;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using EmployeeMonitoringAPI.Data;
using EmployeeMonitoringAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using EmployeeMonitoringAPI.Helpers;

var builder = WebApplication.CreateBuilder(args);

// ======================
// Add Services
// ======================

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<MfaService>();
builder.Services.AddHostedService<FridayTimesheetReminderService>();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<MfaOptions>(builder.Configuration.GetSection(MfaOptions.SectionName));
if (builder.Environment.IsDevelopment()) builder.Services.AddScoped<ISmsSender, DevelopmentSmsSender>();
else builder.Services.AddScoped<ISmsSender, UnconfiguredProductionSmsSender>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth-login", limiter => { limiter.PermitLimit = 10; limiter.Window = TimeSpan.FromMinutes(1); limiter.QueueLimit = 0; });
    options.AddFixedWindowLimiter("mfa-verify", limiter => { limiter.PermitLimit = 10; limiter.Window = TimeSpan.FromMinutes(1); limiter.QueueLimit = 0; });
    options.AddFixedWindowLimiter("mfa-resend", limiter => { limiter.PermitLimit = 5; limiter.Window = TimeSpan.FromMinutes(5); limiter.QueueLimit = 0; });
    options.AddFixedWindowLimiter("password-change", limiter => { limiter.PermitLimit = 5; limiter.Window = TimeSpan.FromMinutes(15); limiter.QueueLimit = 0; });
    options.AddFixedWindowLimiter("attendance-action", limiter => { limiter.PermitLimit = 12; limiter.Window = TimeSpan.FromMinutes(1); limiter.QueueLimit = 0; });
});
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ======================
// Swagger
// ======================

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Employee Monitoring API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Token like: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ======================
// CORS
// ======================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ======================
// Database
// ======================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// ======================
// JWT Authentication
// ======================

var jwtSettings = builder.Configuration.GetSection("Jwt");

var jwtKey = jwtSettings["Key"];

if (string.IsNullOrEmpty(jwtKey))
{
    throw new Exception("JWT Key is missing in appsettings.json");
}

var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;

        options.SaveToken = true;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(key),

                NameClaimType = ClaimTypes.NameIdentifier,
                RoleClaimType = ClaimTypes.Role
            };
    });

builder.Services.AddAuthorization();

// ======================
// Build App
// ======================

var app = builder.Build();

// ======================
// Middleware
// ======================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}
app.UseMiddleware<ExceptionMiddleware>();

// CORS must run before HTTPS redirection so browser OPTIONS preflight requests are not redirected.
app.UseCors("AllowAll");

if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRateLimiter();

app.UseAuthentication();

app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true && int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
    {
        var database = context.RequestServices.GetRequiredService<ApplicationDbContext>();
        if (!await database.Users.AsNoTracking().AnyAsync(x => x.UserId == userId && x.IsActive))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "This account is inactive or access has been revoked." });
            return;
        }
    }
    await next();
});

app.UseAuthorization();

app.MapControllers();

app.Run();