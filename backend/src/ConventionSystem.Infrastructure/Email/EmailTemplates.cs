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
}
