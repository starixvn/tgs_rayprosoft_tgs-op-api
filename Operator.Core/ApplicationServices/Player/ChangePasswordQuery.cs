using System;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record ChangePasswordQuery(
	Guid BrandId,
	string PlayerCode,
	string OldPassword,
	string NewPassword);