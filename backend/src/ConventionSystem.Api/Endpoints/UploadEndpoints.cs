using ConventionSystem.Application.Common;
using ConventionSystem.Infrastructure.FileStorage;
using ConventionSystem.Infrastructure.MultiTenancy;
using Microsoft.Extensions.Options;

namespace ConventionSystem.Api.Endpoints;

public static class UploadEndpoints
{
    private static readonly IReadOnlyDictionary<string, string> AllowedImageContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/gif"] = ".gif",
            ["image/webp"] = ".webp",
        };

    public static void MapUploadEndpoints(this RouteGroups groups)
    {
        groups.Authenticated.MapPost("/api/uploads",
            async (
                HttpRequest request,
                IFileStorage storage,
                ITenantContext tenantContext,
                IOptions<FileStorageOptions> options,
                CancellationToken ct) =>
            {
                if (!request.HasFormContentType)
                    return Results.BadRequest(new UploadError("multipart_required", "Filen måste skickas som multipart/form-data."));

                var form = await request.ReadFormAsync(ct);
                var file = form.Files.GetFile("file");
                if (file is null)
                    return Results.BadRequest(new UploadError("file_required", "Fältet 'file' saknas."));

                if (file.Length <= 0)
                    return Results.BadRequest(new UploadError("file_empty", "Filen är tom."));

                var maxBytes = Math.Max(1, options.Value.MaxSizeMb) * 1024L * 1024L;
                if (file.Length > maxBytes)
                    return Results.BadRequest(new UploadError("file_too_large", $"Filen får vara högst {options.Value.MaxSizeMb} MB."));

                if (!AllowedImageContentTypes.ContainsKey(file.ContentType))
                    return Results.BadRequest(new UploadError("invalid_file_type", "Filtypen stöds inte."));

                await using var stream = file.OpenReadStream();
                var url = await storage.UploadAsync(
                    tenantContext.TenantId.ToString("N"),
                    file.FileName,
                    stream,
                    file.ContentType,
                    ct);

                return Results.Ok(new UploadResponse(url));
            });
    }
}

public sealed record UploadResponse(string Url);
public sealed record UploadError(string ErrorCode, string Message);
