namespace ConventionSystem.Domain.Convention;

public static class EditionContentKey
{
    public const string HeroTitle = "hero.title";
    public const string HeroIngress = "hero.ingress";
    public const string CtaVisitorLabel = "cta.visitor.label";
    public const string CtaOrganiserLabel = "cta.organiser.label";
    public const string CtaStaffLabel = "cta.staff.label";

    public static readonly IReadOnlySet<string> AllKeys = new HashSet<string>
    {
        HeroTitle, HeroIngress, CtaVisitorLabel, CtaOrganiserLabel, CtaStaffLabel
    };
}
