using System.Text.Json;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class GetUmPayExchangeRateResponse
{
	public JsonDocument Rate { get; set; }
}
