using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Models.Messages;
using TGS.Services.Operator.StationLetters;
using TGS.Services.Operator.ApplicationServices.StationLetters;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/letter")]
[ApiController]
public class StationLetterController : ControllerBase
{
    /// <summary>
    ///     Gets the station letters.
    /// </summary>
    /// <param name="getStationLetterService">The list station letter service.</param>
    /// <param name="countStationLetterService">The count station letter service.</param>
    /// <param name="request">The request.</param>
    /// <param name="language">language</param>
    /// <returns></returns>
    [HttpGet("{language}")]
    [AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
    public async Task<PagingResponse<StationLetterResponse>> GetLettersAsync(
        [FromServices] IQueryService<GetStationLettersQuery, StationLetter> getStationLetterService,
        [FromServices] IGetService<GetStationLettersQuery, int> countStationLetterService,
        [FromQuery] PagingRequest request,
        string language)
    {
        var playerTokenData = HttpContext.Items.GetPlayerTokenData();

        var paging = new PageableRequest();
        if (request.ShowAll)
            paging.ShowAll = true;
        else
        {
            paging.Page = request.Page;
            paging.Rows = request.Rows;
            paging.OrderBy = request.IsDesc ? SortOrder.Descending : SortOrder.Ascending;
            paging.SortBy = request.SortBy;
        }

        var query = new GetStationLettersQuery(playerTokenData.PlayerCode, paging);

        return new()
        {
            TotalCount = await countStationLetterService
                .GetAsync(query)
                .ConfigureAwait(false),
            Items = getStationLetterService.QueryAsync(query)
                .Select(letter => new StationLetterResponse
                {
                    Id = letter.Id,
                    Subject = letter.Subject,
                    Content = letter.Content,
                    CreateOn = letter.CreateOn,
                    IsRead = letter.IsRead,
                    Localization = letter.Localization.ContainsKey(language) == true
                    ? letter.Localization[language]
                    : JsonDocument.Parse("{}")
                })
                .ToEnumerable()
        };
    }

    /// <summary>
    ///     Sets the letter have read.
    /// </summary>
    /// <param name="setHaveReadService">The set have read service.</param>
    /// <param name="stationLetterIds">The station letter ids.</param>
    [HttpPost]
    [AuthorizePlayerToken(AllowNullOrEmpty = false, NeedAuthorize = true)]
    public async Task SetHaveReadAsync(
        [FromServices] ICommandService<SetHaveReadCommand> setHaveReadService,
        [FromBody] IEnumerable<Guid> stationLetterIds)
    {
        var playerTokenData = HttpContext.Items.GetPlayerTokenData();

        foreach (var id in stationLetterIds)
            await setHaveReadService
                .ExecuteAsync(new(playerTokenData.PlayerCode, id))
                .ConfigureAwait(false);
    }
}
