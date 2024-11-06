using System.Globalization;
using System;

namespace TGS.Services.Operator.ApplicationServices.Sms;

public record SendOtpCommand(
	Guid BrandId,
	CultureInfo CultureInfo,
	string AreaCode,
	string Phone);