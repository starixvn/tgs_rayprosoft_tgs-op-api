using System;

namespace TGS.Services.Operator.ApplicationServices.Promotions;
public record SignInRouletteCommand(string PlayerCode, Guid PromotionId);
