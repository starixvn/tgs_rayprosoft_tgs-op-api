using System;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

public record CheckPlayerIdentityCommand(Guid BrandId, string PlayerCode);
