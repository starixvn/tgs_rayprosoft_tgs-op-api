using System;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record CreateIdentityAuthenticationFormCommand(
	string PlayerCode,
	Guid BrandId,
	string FileId);
