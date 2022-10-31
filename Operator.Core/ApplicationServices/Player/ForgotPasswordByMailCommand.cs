using System;
using System.Globalization;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record ForgotPasswordByMailCommand(
	Guid BrandId,
	CultureInfo CultureInfo,
	string Email);
