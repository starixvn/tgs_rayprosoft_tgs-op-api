using System;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record CheckBrandPhoneQuery(Guid BrandId, string? PlayerCode, string AreaCode, string Phone);