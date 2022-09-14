using System;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class JoinedPromotionsResponse
{
    public string PromotionName { get; init; }

    public DateTimeOffset JoinedOn { get; init; }
}