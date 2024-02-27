using Microsoft.AspNetCore.Mvc;
using Tgs.Paging;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Messages;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.GameBalance;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/game/balance")]
[ApiController]
public class GameBalanceController : ControllerBase
{
	/// <summary>
	/// Transfer wallet fund in(All GP -> Tgs)
	/// </summary>
	/// <param name="fundInAllService">The fund in all service.</param>
	/// <returns></returns>
	[HttpPost("fund/in", Name = "FundInAllGameBalance")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public ValueTask FundInAllGameBalanceAsync(
		[FromServices] ICommandService<FundInAllGameBalanceCommand> fundInAllService)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		return fundInAllService
			.ExecuteAsync(new FundInAllGameBalanceCommand(
				brandId,
				playerTokenData.PlayerCode,
				null));
	}

	/// <summary>
	/// Transfer wallet fund in(GP -> Tgs)
	/// </summary>
	/// <param name="submitGameBalanceFundInCommandService"></param>
	/// <param name="gameStatusValidator"></param>
	/// <param name="request"></param>
	/// <returns></returns>
	[HttpPut("fund/in", Name = "FundInGameBalance")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async ValueTask FundInGameBalanceAsync(
		[FromServices] ICommandService<FundInGameBalanceCommand> submitGameBalanceFundInCommandService,
		[FromServices] IGameProviderStatusValidator gameStatusValidator,
		[FromBody] SubmitGameBalanceFundInRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		await gameStatusValidator.CheckGameProviderStatusAsync(playerTokenData, request.GameProviderCode, brandId);

		var command = new FundInGameBalanceCommand
		(
			request.GameProviderCode,
			playerTokenData.PlayerCode,
			request.Amount
		);

		await submitGameBalanceFundInCommandService
			.ExecuteAsync(command)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Get transfer wallet balance
	/// </summary>
	/// <param name="getGameBalanceService"></param>
	/// <param name="gameStatusValidator"></param>
	/// <param name="gpCode"></param>
	/// <returns></returns>
	[HttpGet("{gpCode}", Name = "GetGameBalance")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<GetGameBalanceResponse> GetGameBalanceAsync(
		[FromServices] IGetService<GetGameBalanceQuery, decimal> getGameBalanceService,
		[FromServices] IGameProviderStatusValidator gameStatusValidator,
		string gpCode)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		try
		{
			await gameStatusValidator.CheckGameProviderStatusAsync(
				playerTokenData,
				gpCode,
				brandId).ConfigureAwait(false);

			var query = new GetGameBalanceQuery(playerTokenData.PlayerCode, gpCode);

			var balance = await getGameBalanceService.GetAsync(
				query,
				HttpContext.RequestAborted).ConfigureAwait(false);

			return new GetGameBalanceResponse
			{
				Balance = balance,
				Error = (int)TgsApiError.None,
				ErrorMessage = string.Empty
			};
		}
		catch
		{
			return new GetGameBalanceResponse
			{
				Balance = 0,
				Error = (int)TgsApiError.GameBalanceFailed,
				ErrorMessage = TgsApiError.GameBalanceFailed.ToString()
			};
		}
	}

	[HttpGet("list", Name = "ListGameBalance")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async Task<PagedResponse<GameBalanceInfosViewModel>> GetGameBalanceInfosAsync(
		[FromServices] IQueryService<GetGameBalanceInfosQuery, GameBalanceRecord> queryService,
		[FromQuery] GetGameBalanceInfosRequest request,
		[FromQuery] PagingRequest pageableRequest)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		var recordOnRange = ((DateTimeOffset Begin, DateTimeOffset End)?)null;

		if (request.BeginRecordOn != null && request.EndRecordOn != null)
			recordOnRange = (request.BeginRecordOn.Value, request.EndRecordOn.Value);

		var query = new GetGameBalanceInfosQuery(brandId, playerTokenData.PlayerCode, recordOnRange);

		var gameBalanceRecords = queryService.QueryAsync(query);

		var paging = new Paging<GameBalanceInfosViewModel>();

		if (!string.IsNullOrWhiteSpace(pageableRequest.SortBy))
		{
			paging.SortByPropertyName(pageableRequest.SortBy, pageableRequest.IsDesc);
		}

		if (pageableRequest.ShowAll)
		{
			return new PagedResponse<GameBalanceInfosViewModel>
			{
				TotalCount = await gameBalanceRecords.CountAsync().ConfigureAwait(false),
				Items = await gameBalanceRecords
					.Select(x => new GameBalanceInfosViewModel(x))
					.ApplyOrderBy(paging)
					.ToArrayAsync()
					.ConfigureAwait(false)
			};
		}

		paging.Page = pageableRequest.Page;
		paging.RowCount = pageableRequest.Rows;

		return new PagedResponse<GameBalanceInfosViewModel>()
		{
			TotalCount = await gameBalanceRecords.CountAsync().ConfigureAwait(false),
			Items = await gameBalanceRecords
				.Select(x => new GameBalanceInfosViewModel(x))
				.ApplyOrderBy(paging)
				.ApplyPaging(paging)
				.ToArrayAsync()
				.ConfigureAwait(false)
		};
	}

	/// <summary>
	/// Transfer wallet fund out(Tgs -> Gp)
	/// </summary>
	/// <param name="submitGameBalanceFundOutCommandService"></param>
	/// <param name="gameStatusValidator"></param>
	/// <param name="request"></param>
	/// <returns></returns>
	[HttpPut("fund/out", Name = "FundOutGameBalance")]
	[AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
	public async ValueTask SubmitGameBalanceFundOutRequestAsync(
		[FromServices] ICommandService<FundOutGameBalanceCommand> submitGameBalanceFundOutCommandService,
		[FromServices] IGameProviderStatusValidator gameStatusValidator,
		[FromBody] SubmitGameBalanceFundOutRequest request)
	{
		var playerTokenData = HttpContext.Items.GetPlayerTokenData();
		var brandId = HttpContext.Items.GetBrandId();

		await gameStatusValidator.CheckGameProviderStatusAsync(playerTokenData, request.GameProviderCode, brandId);

		var command = new FundOutGameBalanceCommand
		(
			request.GameProviderCode,
			playerTokenData.PlayerCode,
			request.Amount
		);

		await submitGameBalanceFundOutCommandService
			.ExecuteAsync(command)
			.ConfigureAwait(false);
	}
}