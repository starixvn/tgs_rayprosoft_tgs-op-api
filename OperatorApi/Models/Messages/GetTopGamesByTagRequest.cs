using System.ComponentModel.DataAnnotations;
using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class GetTopGamesByTagRequest
{
	public int PageSize { get; init; }

	public int PageIndex { get; init; }

	[Required]
	public PlatformType PlatformType { get; init; }

	[Required]
	public GameTag[] Tags { get; init; }

	public string[] ProviderCode { get; init; }

	public GameType[] GameTypeExclude { get; init; }
}
