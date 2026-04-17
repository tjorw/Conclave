namespace ConventionSystem.Infrastructure.Email;

internal static class EmailTemplates
{
    public static (string Subject, string Body) VisitorRegistrationConfirmed()
        => (
            "Din besoksregistrering ar bekraftad",
            "Hej!\n\nDin besoksregistrering ar nu bekraftad.\n\nVanliga halsningar,\nKonventteamet");

    public static (string Subject, string Body) StaffApplicationReceived()
        => (
            "Vi har tagit emot din staffansokan",
            "Hej!\n\nTack for din ansokan till staff. Vi aterkommer sa snart vi kan.\n\nVanliga halsningar,\nKonventteamet");

    public static (string Subject, string Body) StaffApplicationAccepted()
        => (
            "Din staffansokan ar godkand",
            "Hej!\n\nDin staffansokan har godkants. Varmt valkommen!\n\nVanliga halsningar,\nKonventteamet");

    public static (string Subject, string Body) StaffApplicationRejected()
        => (
            "Din staffansokan ar inte godkand",
            "Hej!\n\nTack for din ansokan. Tyvarr kan vi inte ga vidare med den just nu.\n\nVanliga halsningar,\nKonventteamet");

    public static (string Subject, string Body) EventApproved(string eventTitle)
        => (
            "Ditt evenemang ar godkant",
            $"Hej!\n\nDitt evenemang '{eventTitle}' ar godkant.\n\nVanliga halsningar,\nKonventteamet");

    public static (string Subject, string Body) EventRejected(string eventTitle, string comment)
        => (
            "Ditt evenemang behovde justeras",
            $"Hej!\n\nDitt evenemang '{eventTitle}' kunde inte godkannas i nuvarande form.\nKommentar: {comment}\n\nVanliga halsningar,\nKonventteamet");

    public static (string Subject, string Body) PasswordReset(string resetLink)
        => (
            "Aterstall ditt losenord",
            $"Hej!\n\nDu kan aterstalla ditt losenord via den har lank: {resetLink}\n\nOm du inte begarde detta kan du ignorera mailet.");

    public static (string Subject, string Body) EmailConfirmation(string confirmLink)
        => (
            "Bekrafta din e-postadress",
            $"Hej!\n\nBekrafta din e-postadress via den har lanken: {confirmLink}\n\nVanliga halsningar,\nKonventteamet");

    public static (string Subject, string Body) ResendConfirmation(string confirmLink)
        => (
            "Ny lank for e-postbekraftelse",
            $"Hej!\n\nHar ar en ny lank for att bekrafta din e-postadress: {confirmLink}\n\nVanliga halsningar,\nKonventteamet");

    public static (string Subject, string Body) PasswordChanged()
        => (
            "Ditt losenord har andrats",
            "Hej!\n\nDitt losenord har andrats. Om det inte var du, kontakta support omgaende.");
}
