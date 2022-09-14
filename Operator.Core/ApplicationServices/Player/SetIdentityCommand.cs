using System;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record SetIdentityCommand(string PlayerCode, string RealName, string Identity, Guid BrandId);
