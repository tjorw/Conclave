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
using ConventionSystem.Infrastructure.FileStorage;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var jwtOptions = JwtOptions.FromConfiguration(builder.Configuration);

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
builder.Services.AddSingleton<IOptions<JwtOptions>>(sp =>
    Options.Create(JwtOptions.FromConfiguration(sp.GetRequiredService<IConfiguration>())));
builder.Services.AddSingleton<IJwtTokenIssuer, JwtTokenIssuer>();
builder.Services.AddSingleton<IAuthLinkBuilder, AuthLinkBuilder>();
builder.Services.AddSingleton<ITemporaryPasswordGenerator, TemporaryPasswordGenerator>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = jwtOptions.CreateSigningKey()
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

await app.Services.MigrateInfrastructureDatabasesAsync();

await SystemAdminBootstrapper.SeedAsync(app.Services, app.Configuration);
await SingleTenantBootstrapper.SeedAsync(app.Services, app.Configuration);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var enableDevDataSeeding = app.Configuration.GetValue("DevData:EnableSeeding", false);
if (enableDevDataSeeding)
{
    if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Demo"))
    {
        throw new InvalidOperationException(
            "DevData-seeding får bara aktiveras i Development eller Demo.");
    }

    await DevDataSeeder.SeedAsync(app.Services, app.Configuration);
}

app.UseExceptionHandler();
if (app.Configuration.GetValue("UseHttpsRedirect", true))
    app.UseHttpsRedirection();

var webRootPath = app.Environment.WebRootPath;
var fileStorageOptions = app.Services.GetRequiredService<IOptions<FileStorageOptions>>().Value;
var uploadsRootPath = string.IsNullOrWhiteSpace(fileStorageOptions.LocalRootPath)
    ? Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads")
    : fileStorageOptions.LocalRootPath;
Directory.CreateDirectory(uploadsRootPath);

var publicIndexPath = webRootPath is null ? null : Path.Combine(webRootPath, "index.html");
var adminIndexPath = webRootPath is null ? null : Path.Combine(webRootPath, "admin", "index.html");
var portalIndexPath = webRootPath is null ? null : Path.Combine(webRootPath, "portal", "index.html");
var receptionIndexPath = webRootPath is null ? null : Path.Combine(webRootPath, "reception", "index.html");

if (webRootPath is not null && Directory.Exists(webRootPath))
{
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = new PhysicalFileProvider(webRootPath)
    });
    app.UseStaticFiles();
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRootPath),
    RequestPath = "/uploads"
});

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
groups.MapUploadEndpoints();
groups.MapPageEndpoints();
groups.MapMailTemplateEndpoints();
groups.MapSystemTenantEndpoints();

if (publicIndexPath is not null && File.Exists(publicIndexPath))
{
    app.MapFallbackToFile("index.html");
}

if (adminIndexPath is not null && File.Exists(adminIndexPath))
{
    app.MapFallbackToFile("/admin/{*path:nonfile}", "admin/index.html");
}

if (portalIndexPath is not null && File.Exists(portalIndexPath))
{
    app.MapFallbackToFile("/portal/{*path:nonfile}", "portal/index.html");
}

if (receptionIndexPath is not null && File.Exists(receptionIndexPath))
{
    app.MapFallbackToFile("/reception/{*path:nonfile}", "reception/index.html");
}

app.Run();

// Behövs för WebApplicationFactory i integrationstester
public partial class Program { }
