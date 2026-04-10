using ConventionSystem.Application.Staff.Commands.AssignPersonToShift;
using ConventionSystem.Application.Staff.Commands.CancelAssignment;
using ConventionSystem.Application.Staff.Commands.CancelShift;
using ConventionSystem.Application.Staff.Commands.ConfirmAssignment;
using ConventionSystem.Application.Staff.Commands.CreateShift;
using ConventionSystem.Application.Staff.Commands.RejectAssignment;
using ConventionSystem.Application.Staff.Queries.GetShift;
using ConventionSystem.Application.Staff.Queries.ListShifts;
using MediatR;

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
                    request.MinPersons, request.MaxPersons, request.PerformedById), ct);
                return Results.Created($"/shifts/{id}", new { id });
            });

        app.MapPost("/shifts/{shiftId:guid}/assignments",
            async (Guid shiftId, AssignPersonRequest request, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(new AssignPersonToShiftCommand(shiftId, request.PersonId, request.PerformedById), ct);
                return Results.Created($"/assignments/{id}", new { id });
            });

        app.MapPost("/shifts/{shiftId:guid}/assignments/{assignmentId:guid}/confirm",
            async (Guid shiftId, Guid assignmentId, PerformedByRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new ConfirmAssignmentCommand(shiftId, assignmentId, request.PerformedById), ct);
                return Results.NoContent();
            });

        app.MapPost("/shifts/{shiftId:guid}/assignments/{assignmentId:guid}/reject",
            async (Guid shiftId, Guid assignmentId, PerformedByRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new RejectAssignmentCommand(shiftId, assignmentId, request.PerformedById), ct);
                return Results.NoContent();
            });

        app.MapDelete("/shifts/{shiftId:guid}/assignments/{assignmentId:guid}",
            async (Guid shiftId, Guid assignmentId, PerformedByRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CancelAssignmentCommand(shiftId, assignmentId, request.PerformedById), ct);
                return Results.NoContent();
            });

        app.MapPost("/shifts/{shiftId:guid}/cancel",
            async (Guid shiftId, PerformedByRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CancelShiftCommand(shiftId, request.PerformedById), ct);
                return Results.NoContent();
            });

        return app;
    }
}

public record CreateShiftRequest(
    Guid ResponsibleId,
    DateTime StartTime,
    DateTime EndTime,
    int MinPersons,
    int MaxPersons,
    Guid PerformedById);

public record AssignPersonRequest(Guid PersonId, Guid PerformedById);
public record PerformedByRequest(Guid PerformedById);
