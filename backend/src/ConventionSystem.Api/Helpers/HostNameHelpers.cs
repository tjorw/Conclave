namespace ConventionSystem.Api.Helpers;

public static class HostNameHelpers
{
    public static string? TryExtractSubdomain(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return null;

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return null;

        if (Uri.CheckHostName(host) is UriHostNameType.IPv4 or UriHostNameType.IPv6)
            return null;

        var segments = host.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length < 2)
            return null;

        return segments[0].ToLowerInvariant();
    }
}
