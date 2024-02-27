using System;

namespace TGS.Services.Operator.ApplicationServices.GameBalance;

public record FundOutBeforeEnterGameCommand(Guid BrandId, string PlayerCode, string GpCode);