using System;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record CheckBrandPhoneCanUseQuery(Guid BrandId, string? PlayerCode, string AreaCode, string Phone);