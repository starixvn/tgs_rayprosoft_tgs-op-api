using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Tgs.Core.Infrastructures.Security;
using Tgs.ServiceUtility;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Tokens;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class PlayerTokenParserCommandService : IGetService<PlayerTokenParserCommand, PlayerTokenData?>
{
	private readonly IJwtTokenHandler _tokenHandler;

	public PlayerTokenParserCommandService(
		IJwtTokenHandler tokenHandler)
	{
		_tokenHandler = tokenHandler ?? throw new ArgumentNullException(nameof(tokenHandler));
	}

	public ValueTask<PlayerTokenData?> GetAsync(
		PlayerTokenParserCommand command, 
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(command.PlayerToken))
			return ValueTask.FromResult((PlayerTokenData?) null);

		var jwtToken = _tokenHandler.ReadToken(command.PlayerToken);

		var claims = jwtToken.Claims.ToDictionary(c => c.Type, c => c.Value);

		if (!(claims.TryGetValue("sub", out var playerCode) && !string.IsNullOrEmpty(playerCode))
		    || !(claims.TryGetValue("login_id", out var loginId) && !string.IsNullOrEmpty(loginId))
		    || !(claims.TryGetValue("platform_type", out var platform) && !string.IsNullOrEmpty(platform))
		    || !(claims.TryGetValue("exp", out var expire) && !string.IsNullOrEmpty(expire)))
		{
			return ValueTask.FromResult((PlayerTokenData?)null);
		}

		var playerTokenData = new PlayerTokenData
		{
			PlayerCode = playerCode,
			LoginRecordId = Guid.Parse(loginId),
			PlatformType = Enum.Parse<PlatformType>(platform),
			ExpiredOn = new DateTimeOffset(EpochTime.DateTime(long.Parse(expire)))
		};

		return ValueTask.FromResult((PlayerTokenData?) playerTokenData);
	}
}
