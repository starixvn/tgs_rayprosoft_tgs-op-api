using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class GetRecentBigWinRequest
{
	[Required]
	[BindProperty(Name = "platformtype")]
	public PlatformType PlatformType { get; init; }
}
