using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class GetRecommendedGamesByTypesRequest
{
	[BindProperty(Name = "limit", SupportsGet = true)]
	public int Limit { get; init; }

	[Required]
	[BindProperty(Name = "platformtype", SupportsGet = true)]
	public PlatformType PlatformType { get; init; }

	[BindProperty(Name = "gametype")]
	public GameType[] Types { get; init; }
}
