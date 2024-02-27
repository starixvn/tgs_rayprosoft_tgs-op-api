using System;

namespace TGS.Services.Operator.ApplicationServices.GameBalance;

public record FundInAllGameBalanceCommand(Guid BrandId, string PlayerCode, string? IgnoreGpCode);