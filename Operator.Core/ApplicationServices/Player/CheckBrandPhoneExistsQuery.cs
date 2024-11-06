using System;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record CheckBrandPhoneExistsQuery(Guid BrandId, string AreaCode, string Phone);