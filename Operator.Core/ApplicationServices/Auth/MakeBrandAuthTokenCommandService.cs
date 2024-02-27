using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Core.Infrastructures.Security;
using Tgs.ServiceUtility;
using TGS.Core.Models;

namespace TGS.Services.Operator.ApplicationServices.Auth;

internal class MakeBrandAuthTokenCommandService(IJwtTokenHandler jwtTokenHandler) : IGetService<MakeBrandAuthTokenCommand, AuthInfo>
{
	private readonly IJwtTokenHandler _jwtTokenHandler = jwtTokenHandler ?? throw new ArgumentNullException(nameof(jwtTokenHandler));

	public ValueTask<AuthInfo> GetAsync(
		MakeBrandAuthTokenCommand query, CancellationToken cancellationToken = default)
	{
		var secretKey = "abcd";

		var currency = query.Identity.FindFirst("curr")!.Value;
		var signature = $"{query.BrandId}:{currency}:{secretKey}";
		var expires = DateTime.Now.AddSeconds(Consts.TokenExpireSeconds.Jwt);

		var token = _jwtTokenHandler.GenerateToken(
			query.Identity,
			Encoding.UTF8.GetBytes(signature),
			query.Site,
			expires);

		return ValueTask.FromResult(new AuthInfo(
			token,
			new DateTimeOffset(expires).ToUnixTimeMilliseconds()));
	}
}