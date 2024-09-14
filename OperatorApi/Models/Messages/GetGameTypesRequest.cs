using System.ComponentModel.DataAnnotations;
using TGS.Core.Models.Enums;
using TGS.Services.Operator.ApplicationServices.GameTypes;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record GetGameTypesRequest
{
	public int PageIndex { get; init; }

	public int PageSize { get; init; }

	[Required]
	public PlatformType PlatformType { get; init; }

	[Required]
	public string Language { get; init; }

	[Required]
	public string CurrencyCode { get; init; }

	public string[] ProviderCode { get; set; }

	public GameType[] GameTypes { get; set; }

	public GameTag Tag { get; init; } = GameTag.None;

	public OrderType? OrderType { get; init; }
}
