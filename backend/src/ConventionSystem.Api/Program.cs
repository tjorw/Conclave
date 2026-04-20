using ConventionSystem.Api.Auth;
using ConventionSystem.Api.Bootstrap;
using ConventionSystem.Api.Middleware;
using ConventionSystem.Application;
using ConventionSystem.Application.Common;
using ConventionSystem.Api.DevData;
using ConventionSystem.Api.Endpoints;
using ConventionSystem.Api.Services;
using ConventionSystem.Infrastructure;
using ConventionSystem.Infrastructure.Identity;
using ConventionSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddIdentityCore<ApplicationUser>().AddDefaultTokenProviders();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT-nyckel saknas i konfigurationen (Jwt:Key).");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthConstants.Policies.IsAdmin, policy =>
    policy.RequireClaim(AuthConstants.Claims.IsAdmin, AuthConstants.Claims.IsAdminTrue));

    options.AddPolicy(AuthConstants.Policies.IsSystemAdmin, policy =>
        policy.RequireClaim(AuthConstants.Claims.IsSystemAdmin, AuthConstants.Claims.IsSystemAdminTrue));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy
            .WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:4200", "http://localhost:4201"])
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

// Migrera databaser automatiskt vid uppstart
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<ConventionDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<ApplicationIdentityDbContext>().Database.MigrateAsync();
}

await SystemAdminBootstrapper.SeedAsync(app.Services, app.Configuration);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    var enableDevDataSeeding = app.Configuration.GetValue("DevData:EnableSeeding", true);
    if (enableDevDataSeeding)
    {
        await DevDataSeeder.SeedAsync(app.Services, app.Configuration);
    }
}

app.UseExceptionHandler();
if (app.Configuration.GetValue("UseHttpsRedirect", true))
    app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

var groups = new RouteGroups(
    Anonymous:     app.MapGroup(""),
    Authenticated: app.MapGroup("").RequireAuthorization(),
    Admin:         app.MapGroup("").RequireAuthorization(AuthConstants.Policies.IsAdmin),
    SystemAdmin:   app.MapGroup("").RequireAuthorization(AuthConstants.Policies.IsSystemAdmin)
);

groups.MapAuthEndpoints();
groups.MapMeEndpoints();
groups.MapFeedEndpoints();
groups.MapConventionEndpoints();
groups.MapPersonEndpoints();
groups.MapEditionEndpoints();
groups.MapShiftEndpoints();
groups.MapRegistrationEndpoints();
groups.MapEventEndpoints();
groups.MapSystemTenantEndpoints();

app.Run();

// Behövs för WebApplicationFactory i integrationstester
public partial class Program { }
