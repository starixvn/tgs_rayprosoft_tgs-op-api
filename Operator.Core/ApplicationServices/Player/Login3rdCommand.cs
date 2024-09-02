using System;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record Login3rdCommand(
	Guid BrandId,
	string UserName,
	string Password,
	string IpAddress,
	string HostName,
	PlatformType PlatformType,
	string Site,
	bool login3rd);