using System;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record CommitInitPlayerPhoneCommand(
	string SecurityKey,
	string ValidateCode,
	string IpAddress,
	string HostName,
	Guid BrandId);
