using System;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record GetPlayerQuery(Guid BrandId, string PlayerCode);
