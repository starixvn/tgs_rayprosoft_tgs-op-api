using System;

namespace TGS.Services.PlayerBucket.Models;

public record CreatePlayerBucketWithDepositRequest(
    Guid BrandId,
    Guid? PromotionId,
    string PlayerCode,
    Guid DefaultWalletId,
    decimal DepositAmount,
    string Remark,
    Guid FormId,
    Guid? UserId);
