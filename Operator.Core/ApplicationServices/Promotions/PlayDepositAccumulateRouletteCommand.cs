using System;

namespace TGS.Services.Operator.ApplicationServices.Promotions;

public record PlayDepositAccumulateRouletteCommand(Guid PromotionId, string PlayerCode);
