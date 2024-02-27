using System.Text.Json.Serialization;
using TGS.Core.Models.Messages;

namespace TGS.Services.Operator;

public record LeePayExchangeRateInfo : TgsResponse
{
	[JsonPropertyName("buyprice")]
	public decimal? BuyPrice { get; init; }
	[JsonPropertyName("sellprice")]
	public decimal? SellPrice { get; init; }
	[JsonPropertyName("biztime")]
	public string? BizTime { get; init; }
}