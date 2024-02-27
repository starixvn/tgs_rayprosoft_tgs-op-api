using System;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record CommitSetPlayerEmailCommand(string PlayerCode, string SecurityKey, string ValidateCode, Guid BrandId);