using System;
using System.Globalization;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record ForgotPasswordBySmsCommand(
	Guid BrandId,
	CultureInfo CultureInfo,
	string AreaCode,
	string Phone);