using System;
using System.IO;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record SubmitIdentityAuthenticationFormCommand(
	Guid BrandId,
	string PlayerCode,
	Guid FormId,
	string ContentType,
	Stream FileStream);