using ConventionSystem.Application;
using ConventionSystem.Application.Common;
using ConventionSystem.Api.Endpoints;
using ConventionSystem.Api.Middleware;
using ConventionSystem.Api.Services;
using ConventionSystem.Infrastructure;
using ConventionSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

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
    options.AddPolicy("IsAdmin", policy =>
        policy.RequireClaim("is_admin", "true"));
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

// Migrera systemdatabaserna automatiskt vid uppstart
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<SystemDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<ApplicationIdentityDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantMiddleware>();

app.MapAuthEndpoints();
app.MapMeEndpoints();
app.MapFeedEndpoints();
app.MapSystemEndpoints();
app.MapConventionEndpoints();
app.MapPersonEndpoints();
app.MapEditionEndpoints();
app.MapShiftEndpoints();
app.MapRegistrationEndpoints();
app.MapEventEndpoints();

app.Run();

// Behövs för WebApplicationFactory i integrationstester
public partial class Program { }
