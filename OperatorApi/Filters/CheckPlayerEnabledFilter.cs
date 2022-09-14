using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Extensions;
using TGS.Apis.OperatorApi.Models;
using TGS.Services.Operator.ApplicationServices.Player;

namespace TGS.Apis.OperatorApi.Filters;

public class CheckPlayerEnabledFilter : IAsyncAuthorizationFilter
{
    private readonly IGetService<GetPlayerStatusQuery, PlayerStatus> _getService;

    public CheckPlayerEnabledFilter(
        IGetService<GetPlayerStatusQuery, PlayerStatus> getService)
    {
        _getService = getService ?? throw new ArgumentNullException(nameof(getService));
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var authAttr = context.ActionDescriptor.EndpointMetadata.OfType<AuthorizePlayerTokenAttribute>().LastOrDefault();

        if (authAttr == null || authAttr.AllowNullOrEmpty)
        {
            return;
        }

        var playerTokenData = context.HttpContext.Items.GetPlayerTokenData();

        var playerCode = playerTokenData.PlayerCode;

        var status = await _getService.GetAsync(new GetPlayerStatusQuery(playerCode));

        if (status == PlayerStatus.Disabled)
        {
            var enableBlockedAttr = context.ActionDescriptor.EndpointMetadata
                .OfType<EnableUserBlockedCheckAttribute>().LastOrDefault();

            if (enableBlockedAttr is null)
                return;

            context.Result = new ObjectResult(
                new FailResponse
                {
                    Error = (int)Core.Models.Enums.TgsApiError.UserBlocked,
                    ErrorMessage = string.Empty,
                    ErrorMessageKey = "UserBlocked"
                })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
        else if (status is PlayerStatus.InBlacklist)
        {
            var ignoreBannedAttr = context.ActionDescriptor.EndpointMetadata
                .OfType<IgnoreUserBlacklistCheckAttribute>().LastOrDefault();

            if (ignoreBannedAttr is not null)
                return;

            context.Result = new ObjectResult(
                new FailResponse
                {
                    Error = (int)Core.Models.Enums.TgsApiError.UserBanned,
                    ErrorMessage = string.Empty,
                    ErrorMessageKey = "AccessDenied"
                })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
        else if (status is PlayerStatus.RegisterUndone)
        {
            var ignoreBannedAttr = context.ActionDescriptor.EndpointMetadata
                .OfType<IgnoreUserBlacklistCheckAttribute>().LastOrDefault();

            if (ignoreBannedAttr is not null)
                return;

            context.Result = new ObjectResult(
                new FailResponse
                {
                    Error = (int)Core.Models.Enums.TgsApiError.RegisterUndone,
                    ErrorMessage = string.Empty,
                    ErrorMessageKey = "AccessDenied"
                })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
