using System.IdentityModel.Tokens.Jwt;
using System.Net.Mime;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Auth;

namespace TGS.Apis.OperatorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class AuthController : ControllerBase
{
	[HttpPost("jwt")]
	[AllowAnonymous]
	public async Task<AuthResponse> AuthenticateAsync(
		[FromServices] IGetService<GetAuthQuery, ClaimsIdentity> getAuthQueryService,
		[FromServices] IMaintenanceRedisRepository maintenanceRedisService,
		[FromServices] IGetService<MakeBrandAuthTokenCommand, AuthInfo> makeTokenService,
		[FromBody] AuthRequest request)
	{
		var query = new GetAuthQuery(
			request.ApiKey,
			request.Currency);

		var id = await getAuthQueryService
			.GetAsync(query)
			.ConfigureAwait(false);

		var brandId = Guid.Parse(id.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);

		var isMaintenance = !string.IsNullOrEmpty(
			await maintenanceRedisService
				.GetBrandMaintenanceAsync(brandId)
				.ConfigureAwait(false));

		var tokenInfo = await makeTokenService.GetAsync(
			new MakeBrandAuthTokenCommand(
				brandId,
				id,
				Consts.Site)).ConfigureAwait(false);

		return isMaintenance
			? this.Fail<AuthResponse, AuthResponse>(
				StatusCodes.Status503ServiceUnavailable,
				new AuthResponse()
				{
					Token = tokenInfo.Token,
					ExpiredOn = tokenInfo.ExpiredOn
				})
			: new AuthResponse()
			{
				Token = tokenInfo.Token,
				ExpiredOn = tokenInfo.ExpiredOn
			};
	}
}
