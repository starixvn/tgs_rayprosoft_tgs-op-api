using TGS.Core.Models.Messages;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record GetUBPayExchangeRateResponse : TgsResponse
{
	public decimal Rate { get; set; }
}
