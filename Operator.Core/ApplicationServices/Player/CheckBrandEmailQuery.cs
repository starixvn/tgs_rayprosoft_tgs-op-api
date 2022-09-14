using System;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record CheckBrandEmailQuery(Guid BrandId, string Email, string PlayerCode);
