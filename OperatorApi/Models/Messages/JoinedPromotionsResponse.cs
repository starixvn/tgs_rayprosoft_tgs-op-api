namespace TGS.Apis.OperatorApi.Models.Messages;

public class JoinedPromotionsResponse
{
	public DateTimeOffset JoinedOn { get; init; }

	public string PromotionName { get; init; }
}