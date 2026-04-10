using ConventionSystem.Application;
using ConventionSystem.Application.Common;
using ConventionSystem.Api.Endpoints;
using ConventionSystem.Api.Services;
using ConventionSystem.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapConventionEndpoints();
app.MapPersonEndpoints();
app.MapEditionEndpoints();
app.MapShiftEndpoints();
app.MapRegistrationEndpoints();
app.MapEventEndpoints();

app.Run();
