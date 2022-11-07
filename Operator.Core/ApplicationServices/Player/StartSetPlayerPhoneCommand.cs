using System.Globalization;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record StartSetPlayerPhoneCommand(
	string PlayerCode,
	string AreaCode,
	string Phone,
	CultureInfo CultureInfo,
	bool SmsEnabled);
