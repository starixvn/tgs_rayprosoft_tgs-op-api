using System.Globalization;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record StartSetPlayerEmailCommand(
	string PlayerCode,
	string Email,
	string Brand,
	string Username,
	CultureInfo CultureInfo,
	bool IsRegisterFlow);