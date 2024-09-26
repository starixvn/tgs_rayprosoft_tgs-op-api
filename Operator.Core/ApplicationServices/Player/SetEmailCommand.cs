using System;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record SetEmailCommand(Guid BrandId, string PlayerCode, string Email);