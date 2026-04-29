namespace ConventionSystem.Infrastructure.Email;

internal static class EmailTemplates
{
    public static (string Subject, string Body) VisitorRegistrationConfirmed()
        => (
            "Din besöksregistrering är bekräftad",
            "Hej!\n\nDin besöksregistrering är nu bekräftad.\n\nVänliga hälsningar,\nKonventteamet");

    public static (string Subject, string Body) StaffApplicationReceived()
        => (
            "Vi har tagit emot din funktionärsansökan",
            "Hej!\n\nTack för din ansökan som funktionär. Vi återkommer så snart vi kan.\n\nVänliga hälsningar,\nKonventteamet");

    public static (string Subject, string Body) StaffApplicationAccepted()
        => (
            "Din funktionärsansökan är godkänd",
            "Hej!\n\nDin funktionärsansökan har godkänts. Varmt välkommen!\n\nVänliga hälsningar,\nKonventteamet");

    public static (string Subject, string Body) StaffApplicationRejected()
        => (
            "Din funktionärsansökan är inte godkänd",
            "Hej!\n\nTack för din ansökan. Tyvärr kan vi inte gå vidare med den just nu.\n\nVänliga hälsningar,\nKonventteamet");

    public static (string Subject, string Body) EventApproved(string eventTitle)
        => (
            "Ditt evenemang är godkänt",
            $"Hej!\n\nDitt evenemang '{eventTitle}' är godkänt.\n\nVänliga hälsningar,\nKonventteamet");

    public static (string Subject, string Body) EventRejected(string eventTitle, string comment)
        => (
            "Ditt evenemang behövde justeras",
            $"Hej!\n\nDitt evenemang '{eventTitle}' kunde inte godkännas i nuvarande form.\nKommentar: {comment}\n\nVänliga hälsningar,\nKonventteamet");

    public static (string Subject, string Body) PasswordReset(string resetLink)
        => (
            "Återställ ditt lösenord",
            $"Hej!\n\nDu kan återställa ditt lösenord via den här länken: {resetLink}\n\nOm du inte begärde detta kan du ignorera mejlet.");

    public static (string Subject, string Body) EmailConfirmation(string confirmLink)
        => (
            "Bekräfta din e-postadress",
            $"Hej!\n\nBekräfta din e-postadress via den här länken: {confirmLink}\n\nVänliga hälsningar,\nKonventteamet");

    public static (string Subject, string Body) ResendConfirmation(string confirmLink)
        => (
            "Ny länk för e-postbekräftelse",
            $"Hej!\n\nHär är en ny länk för att bekräfta din e-postadress: {confirmLink}\n\nVänliga hälsningar,\nKonventteamet");

    public static (string Subject, string Body) PasswordChanged()
        => (
            "Ditt lösenord har ändrats",
            "Hej!\n\nDitt lösenord har ändrats. Om det inte var du, kontakta support omgående.");

    public static (string Subject, string Body) TenantSignupWelcome(
        string organizationName,
        string subdomain,
        string temporaryPassword,
        string confirmLink)
        => (
            $"Välkommen till Conclave - {organizationName}",
            "Hej!\n\n" +
            $"Din tenant '{organizationName}' har skapats med subdomanen '{subdomain}'.\n" +
            "Bekräfta först din e-postadress via länken nedan för att aktivera tenanten:\n" +
            $"{confirmLink}\n\n" +
            "När bekräftelsen är klar kan du logga in med den här temporära lösenordsuppgiften:\n" +
            $"{temporaryPassword}\n\n" +
            "Byt lösenord efter första inloggningen.\n\n" +
            "Vänliga hälsningar,\nConclave");

    public static (string Subject, string Body) TenantProvisionedWelcome(
        string organizationName,
        string subdomain,
        string toEmail,
        string temporaryPassword,
        string loginLink)
        => (
            $"VÃ¤lkommen till Conclave Admin - {organizationName}",
            "Hej!\n\n" +
            $"Ditt konvent '{organizationName}' har nu provisionerats med subdomÃ¤nen '{subdomain}'.\n" +
            "Du kan logga in som konventsadmin via lÃ¤nken nedan:\n" +
            $"{loginLink}\n\n" +
            "Dina inloggningsuppgifter Ã¤r:\n" +
            $"E-post: {toEmail}\n" +
            $"LÃ¶senord: {temporaryPassword}\n\n" +
            "Byt lÃ¶senord efter fÃ¶rsta inloggningen.\n\n" +
            "VÃ¤nliga hÃ¤lsningar,\nConclave");
}
