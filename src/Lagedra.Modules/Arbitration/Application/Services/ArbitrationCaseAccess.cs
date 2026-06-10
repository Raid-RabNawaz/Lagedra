using Lagedra.Modules.Arbitration.Domain.Aggregates;
using Lagedra.SharedKernel.Integration;

namespace Lagedra.Modules.Arbitration.Application.Services;

public enum CaseAccessLevel
{
    View,
    AttachEvidence,
    DecideOrClose,
    Appeal
}

public static class ArbitrationCaseAccess
{
    public static bool IsDealParty(Guid userId, ArbitrationCase arbitrationCase, DealParticipantsDto participants) =>
        arbitrationCase.FiledByUserId == userId
        || participants.LandlordUserId == userId
        || participants.TenantUserId == userId;

    public static bool IsAssignedArbitrator(Guid userId, ArbitrationCase arbitrationCase) =>
        arbitrationCase.ArbitratorAssignments.Any(a => a.ArbitratorUserId == userId);

    public static bool Allows(
        CaseAccessLevel level,
        ArbitrationUserContext caller,
        ArbitrationCase arbitrationCase,
        DealParticipantsDto participants)
    {
        if (caller.IsPlatformAdmin)
        {
            return true;
        }

        return level switch
        {
            CaseAccessLevel.View => (caller.IsArbitrator && IsAssignedArbitrator(caller.UserId, arbitrationCase))
                || IsDealParty(caller.UserId, arbitrationCase, participants),
            CaseAccessLevel.AttachEvidence => IsDealParty(caller.UserId, arbitrationCase, participants),
            CaseAccessLevel.DecideOrClose => caller.IsArbitrator
                && IsAssignedArbitrator(caller.UserId, arbitrationCase),
            CaseAccessLevel.Appeal => IsDealParty(caller.UserId, arbitrationCase, participants),
            _ => false
        };
    }

    public static bool IsVisibleTo(
        ArbitrationUserContext caller,
        ArbitrationCase arbitrationCase,
        DealParticipantsDto? participants)
    {
        if (caller.IsPlatformAdmin)
        {
            return true;
        }

        if (caller.IsArbitrator && IsAssignedArbitrator(caller.UserId, arbitrationCase))
        {
            return true;
        }

        return participants is not null
            && IsDealParty(caller.UserId, arbitrationCase, participants);
    }
}
