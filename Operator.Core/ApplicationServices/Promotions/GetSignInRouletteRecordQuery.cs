using System;

namespace TGS.Services.Operator.ApplicationServices.Promotions;
public record GetSignInRouletteRecordQuery(string PlayerCode, Guid PromotionId);
