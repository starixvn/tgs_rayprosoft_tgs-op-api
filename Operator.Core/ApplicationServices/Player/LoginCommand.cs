using System;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record LoginCommand(
	Guid BrandId,
	string UserName,
	string Password,
	string IpAddress,
	string HostName,
	PlatformType PlatformType,
	string Site);
