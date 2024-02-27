using System;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record CommitInitPlayerEmailCommand(
	string SecurityKey,
	string ValidateCode,
	Guid BrandId);