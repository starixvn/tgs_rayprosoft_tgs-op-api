using System;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record CheckPlayerBannedQuery(Guid BrandId, string UserName);
