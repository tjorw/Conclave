using ConventionSystem.Domain.Content.Enums;

namespace ConventionSystem.Application.Content;

public static class MailTemplateVariables
{
    private static readonly IReadOnlyDictionary<MailTemplateType, IReadOnlyList<string>> Variables =
        new Dictionary<MailTemplateType, IReadOnlyList<string>>
        {
            [MailTemplateType.VisitorRegistrationConfirmed] = ["firstName", "conventionName"],
            [MailTemplateType.StaffApplicationReceived] = ["firstName", "conventionName"],
            [MailTemplateType.StaffApplicationAccepted] = ["firstName", "conventionName"],
            [MailTemplateType.StaffApplicationRejected] = ["firstName", "conventionName"],
            [MailTemplateType.EventApproved] = ["firstName", "eventTitle", "conventionName"],
            [MailTemplateType.EventRejected] = ["firstName", "eventTitle", "rejectionComment", "conventionName"],
            [MailTemplateType.CoOrganiserInvitation] = ["firstName", "eventTitle", "inviteLink"],
        };

    public static IReadOnlyList<string> GetVariables(MailTemplateType type)
        => Variables.TryGetValue(type, out var vars) ? vars : [];
}
