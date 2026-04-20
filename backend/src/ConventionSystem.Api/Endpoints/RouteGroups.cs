namespace ConventionSystem.Api.Endpoints;

public sealed record RouteGroups(
    RouteGroupBuilder Anonymous,
    RouteGroupBuilder Authenticated,
    RouteGroupBuilder Admin,
    RouteGroupBuilder SystemAdmin
);
