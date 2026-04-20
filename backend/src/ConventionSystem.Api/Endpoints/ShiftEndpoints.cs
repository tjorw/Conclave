using ConventionSystem.Application.Staff.Commands.AssignPersonToShift;
using ConventionSystem.Application.Staff.Commands.CancelAssignment;
using ConventionSystem.Application.Staff.Commands.CancelShift;
using ConventionSystem.Application.Staff.Commands.ConfirmAssignment;
using ConventionSystem.Application.Staff.Commands.CreateShift;
using ConventionSystem.Application.Staff.Commands.RejectAssignment;
using ConventionSystem.Application.Staff.Queries.GetShift;
using ConventionSystem.Application.Staff.Queries.ListShifts;
using ConventionSystem.Application.Common;

namespace ConventionSystem.Api.Endpoints;

public static class ShiftEndpoints
{
    public static IEndpointRouteBuilder MapShiftEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/stations/{stationId:guid}/shifts",
            async (Guid stationId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListShiftsQuery(stationId), ct)));

        app.MapGet("/shifts/{shiftId:guid}", async (Guid shiftId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetShiftQuery(shiftId), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        app.MapPost("/stations/{stationId:guid}/shifts",
            async (Guid stationId, CreateShiftRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new CreateShiftCommand(
                    stationId, request.ResponsibleId, request.StartTime, request.EndTime,
                    request.MinPersons, request.MaxPersons), ct);
                return Results.Created($"/shifts/{id}", new { id });
            }).RequireAuthorization();

        app.MapPost("/shifts/{shiftId:guid}/assignments",
            async (Guid shiftId, AssignPersonRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new AssignPersonToShiftCommand(shiftId, request.PersonId), ct);
                return Results.Created($"/assignments/{id}", new { id });
            }).RequireAuthorization();

        app.MapPost("/shifts/{shiftId:guid}/assignments/{assignmentId:guid}/confirm",
            async (Guid shiftId, Guid assignmentId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ConfirmAssignmentCommand(shiftId, assignmentId), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        app.MapPost("/shifts/{shiftId:guid}/assignments/{assignmentId:guid}/reject",
            async (Guid shiftId, Guid assignmentId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RejectAssignmentCommand(shiftId, assignmentId), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        app.MapDelete("/shifts/{shiftId:guid}/assignments/{assignmentId:guid}",
            async (Guid shiftId, Guid assignmentId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CancelAssignmentCommand(shiftId, assignmentId), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        app.MapPost("/shifts/{shiftId:guid}/cancel",
            async (Guid shiftId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CancelShiftCommand(shiftId), ct);
                return Results.NoContent();
            }).RequireAuthorization();

        return app;
    }
}

public record CreateShiftRequest(
    Guid ResponsibleId,
    DateTime StartTime,
    DateTime EndTime,
    int MinPersons,
    int MaxPersons);

public record AssignPersonRequest(Guid PersonId);
