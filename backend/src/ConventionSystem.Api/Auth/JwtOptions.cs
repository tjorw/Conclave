using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ConventionSystem.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const string KeyConfigurationKey = SectionName + ":Key";
    public const string IssuerConfigurationKey = SectionName + ":Issuer";
    public const string AudienceConfigurationKey = SectionName + ":Audience";

    public string Key { get; init; } = "";
    public string Issuer { get; init; } = "";
    public string Audience { get; init; } = "";

    public static JwtOptions FromConfiguration(IConfiguration configuration)
    {
        var options = configuration.GetRequiredSection(SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT-konfiguration saknas.");

        options.Validate();
        return options;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Key))
            throw new InvalidOperationException($"JWT-nyckel saknas i konfigurationen ({KeyConfigurationKey}).");
        if (string.IsNullOrWhiteSpace(Issuer))
            throw new InvalidOperationException($"JWT-issuer saknas i konfigurationen ({IssuerConfigurationKey}).");
        if (string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException($"JWT-audience saknas i konfigurationen ({AudienceConfigurationKey}).");
    }

    public SymmetricSecurityKey CreateSigningKey() =>
        new(Encoding.UTF8.GetBytes(Key));
}
