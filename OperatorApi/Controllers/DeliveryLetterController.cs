using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using Tgs.StationLetterService.Client;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator.ApplicationServices.StationLetters;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/delivery-box")]
[ApiController]
public class DeliveryLetterController : ControllerBase
{
	[HttpGet(Name = "GetPlayerDeliveryLetters")]
	public async Task<PagingResponse<DeliveryLetterListItemViewModel>> GetPlayerDeliveryLettersAsync(
		[FromServices] IQueryService<GetDeliveryLettersQuery, DeliveryLetter> queryService,
		[FromQuery(Name = "page")] int page = 1,
		[FromQuery(Name = "rows")] int rows = 10)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData()!;

		var query = new GetDeliveryLettersQuery(playerTokenData.PlayerCode);

		var result = await queryService.QueryAsync(query, HttpContext.RequestAborted)
			.Select(letter => new DeliveryLetterListItemViewModel
			{
				Id = letter.Id,
				Sender = letter.Sender,
				Subject = letter.Subject,
				SentOn = letter.SentOn
			})
			.ToArrayAsync(HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return new PagingResponse<DeliveryLetterListItemViewModel>
		{
			TotalCount = result.Length,
			Items = result.Chunk(rows).Skip(page - 1).FirstOrDefault() ?? Array.Empty<DeliveryLetterListItemViewModel>()
		};
	}
}
