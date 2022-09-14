using System;

#nullable enable

namespace TGS.Services.Operator.ApplicationServices.Player;

public record CreatePlayerWithPlayerInvitationCommand(
    Guid BrandId,
    string UserName,
    string Password,
    string CurrencyCode,
    string UpperPlayerCode,
    bool IsTestPlayer);
