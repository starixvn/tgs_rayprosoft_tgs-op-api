using System;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record CommitSetPlayerPhoneCommand(Guid BrandId, string PlayerCode, string SecurityKey, string ValidateCode);
