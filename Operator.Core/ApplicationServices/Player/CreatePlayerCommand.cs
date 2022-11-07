using System;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record CreatePlayerCommand(
	Guid BrandId,
	string UserName,
	string Password,
	string CurrencyCode,
	bool IsTestPlayer);
