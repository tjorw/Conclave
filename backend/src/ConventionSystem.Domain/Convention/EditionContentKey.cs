namespace ConventionSystem.Domain.Convention;

public static class EditionContentKey
{
    public const string HeroTitle = "hero.title";
    public const string HeroIngress = "hero.ingress";
    public const string HeroPrimaryActionLabel = "hero.primaryActionLabel";
    public const string CtaVisitorLabel = "cta.visitor.label";
    public const string CtaOrganiserLabel = "cta.organiser.label";
    public const string CtaStaffLabel = "cta.staff.label";
    public const string CtaVisitorDescription = "cta.visitor.description";
    public const string CtaOrganiserDescription = "cta.organiser.description";
    public const string CtaStaffDescription = "cta.staff.description";
    public const string CtaVisitorOpenLabel = "cta.visitor.openLabel";
    public const string CtaOrganiserOpenLabel = "cta.organiser.openLabel";
    public const string CtaStaffOpenLabel = "cta.staff.openLabel";
    public const string CtaVisitorClosedLabel = "cta.visitor.closedLabel";
    public const string CtaOrganiserClosedLabel = "cta.organiser.closedLabel";
    public const string CtaStaffClosedLabel = "cta.staff.closedLabel";
    public const string FeaturedSectionTitle = "featured.sectionTitle";
    public const string FeaturedViewAllLabel = "featured.viewAllLabel";

    public static readonly IReadOnlySet<string> AllKeys = new HashSet<string>
    {
        HeroTitle,
        HeroIngress,
        HeroPrimaryActionLabel,
        CtaVisitorLabel,
        CtaOrganiserLabel,
        CtaStaffLabel,
        CtaVisitorDescription,
        CtaOrganiserDescription,
        CtaStaffDescription,
        CtaVisitorOpenLabel,
        CtaOrganiserOpenLabel,
        CtaStaffOpenLabel,
        CtaVisitorClosedLabel,
        CtaOrganiserClosedLabel,
        CtaStaffClosedLabel,
        FeaturedSectionTitle,
        FeaturedViewAllLabel,
    };
}
