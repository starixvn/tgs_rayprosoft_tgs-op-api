using System.Globalization;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record StartSetPlayerEmailCommand(
	string PlayerCode,
	string Email,
	CultureInfo CultureInfo,
	bool IsRegisterFlow);