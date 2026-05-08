using ConventionSystem.Domain.Content.Enums;

namespace ConventionSystem.Application.Content;

public static class DefaultMailTemplates
{
    public static (string Subject, string BodyMarkdown) GetTemplate(MailTemplateType type)
        => type switch
        {
            MailTemplateType.VisitorRegistrationConfirmed => (
                "Din besöksregistrering är bekräftad",
                "Hej {{firstName}}!\n\nDin besöksregistrering till **{{conventionName}}** är nu bekräftad.\n\nVänliga hälsningar,\nKonventteamet"),

            MailTemplateType.StaffApplicationReceived => (
                "Vi har tagit emot din funktionärsansökan",
                "Hej {{firstName}}!\n\nTack för din ansökan som funktionär på **{{conventionName}}**. Vi återkommer så snart vi kan.\n\nVänliga hälsningar,\nKonventteamet"),

            MailTemplateType.StaffApplicationAccepted => (
                "Din funktionärsansökan är godkänd",
                "Hej {{firstName}}!\n\nDin funktionärsansökan till **{{conventionName}}** har godkänts. Varmt välkommen!\n\nVänliga hälsningar,\nKonventteamet"),

            MailTemplateType.StaffApplicationRejected => (
                "Din funktionärsansökan är inte godkänd",
                "Hej {{firstName}}!\n\nTack för din ansökan till **{{conventionName}}**. Tyvärr kan vi inte gå vidare med den just nu.\n\nVänliga hälsningar,\nKonventteamet"),

            MailTemplateType.EventApproved => (
                "Ditt evenemang är godkänt",
                "Hej {{firstName}}!\n\nDitt evenemang **{{eventTitle}}** på **{{conventionName}}** är godkänt.\n\nVänliga hälsningar,\nKonventteamet"),

            MailTemplateType.EventRejected => (
                "Ditt evenemang behövde justeras",
                "Hej {{firstName}}!\n\nDitt evenemang **{{eventTitle}}** på **{{conventionName}}** kunde inte godkännas i nuvarande form.\n\nKommentar: {{rejectionComment}}\n\nVänliga hälsningar,\nKonventteamet"),

            MailTemplateType.CoOrganiserInvitation => (
                "Du har blivit inbjuden som medarrangör",
                "Hej {{firstName}}!\n\nDu har blivit inbjuden som medarrangör för evenemanget **{{eventTitle}}**.\n\nKlicka på länken nedan för att acceptera inbjudan:\n{{inviteLink}}\n\nOm du inte har ett konto ännu kan du registrera dig via länken ovan.\n\nVänliga hälsningar,\nKonventteamet"),

            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Okänd malltyp.")
        };

    public static IReadOnlyList<MailTemplateType> AllCustomizableTypes =>
    [
        MailTemplateType.VisitorRegistrationConfirmed,
        MailTemplateType.StaffApplicationReceived,
        MailTemplateType.StaffApplicationAccepted,
        MailTemplateType.StaffApplicationRejected,
        MailTemplateType.EventApproved,
        MailTemplateType.EventRejected,
        MailTemplateType.CoOrganiserInvitation,
    ];
}
