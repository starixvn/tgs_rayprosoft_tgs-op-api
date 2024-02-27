using System;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

public record GetRequireBucketAmountQuery(Guid BrandId, string PlayerCode);