using System.Text.Json.Serialization;
using TGS.Core.Models.Messages;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record GetLeePayExchangeRateResponse : TgsResponse
{
	/// <summary>
	/// 交易所收購價格，建議用來充值使用
	/// </summary>
	[JsonPropertyName("buyprice")]
	public string BuyPrice { get; init; }
	/// <summary>
	/// 交易所賣出價格，建議用來出款使用
	/// </summary>
	[JsonPropertyName("sellprice")]
	public string SellPrice { get; set; }
	/// <summary>
	/// 價格更新日期
	/// </summary>
	[JsonPropertyName("updatetime")]
	public string UpdateTime { get; set; }
}