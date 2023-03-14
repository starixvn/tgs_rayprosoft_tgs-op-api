using System;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record PlayerTokenParserCommand(Guid BrandId, string PlayerToken);
