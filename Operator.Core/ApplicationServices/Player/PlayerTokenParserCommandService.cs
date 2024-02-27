using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Tgs.Core.Infrastructures.Security;
using Tgs.ServiceUtility;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Tokens;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class PlayerTokenParserCommandService(
	IJwtTokenHandler tokenHandler,
	IBrandRepository brandRepository,
	ILogger<PlayerTokenParserCommandService> logger) : IGetService<PlayerTokenParserCommand, PlayerTokenData?>
{
	private readonly IBrandRepository _brandRepository = brandRepository ?? throw new ArgumentNullException(nameof(brandRepository));
	private readonly ILogger<PlayerTokenParserCommandService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly IJwtTokenHandler _tokenHandler = tokenHandler ?? throw new ArgumentNullException(nameof(tokenHandler));

	public async ValueTask<PlayerTokenData?> GetAsync(
		PlayerTokenParserCommand command,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(command.PlayerToken))
			return null;

		var secretKey = await _brandRepository.GetSecretKeyAsync(
			command.BrandId,
			cancellationToken).ConfigureAwait(false);

		var (isValid, principal, securityToken) = _tokenHandler.ValidateToken(
			command.PlayerToken,
			new TokenValidationParameters
			{
				ValidateIssuer = true,
				ValidIssuer = "operator-api",
				ValidateAudience = false,
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes($"{secretKey}{secretKey}")),
				ValidateLifetime = true,
				ClockSkew = TimeSpan.Zero
			},
			exception => _logger.LogDebug(exception, "Token validation failed"),
			_ => _logger.LogDebug("jwt validate expired"));

		if (!isValid)
			return null;

		var claims = principal!.Claims.ToDictionary(c => c.Type, c => c.Value);

		var playerCode = claims[ClaimTypes.NameIdentifier];
		var loginId = claims["login_id"];
		var platform = claims["platform_type"];
		var expire = claims["exp"];

		var playerTokenData = new PlayerTokenData
		{
			PlayerCode = playerCode,
			LoginRecordId = Guid.Parse(loginId),
			PlatformType = Enum.Parse<PlatformType>(platform),
			ExpiredOn = new DateTimeOffset(EpochTime.DateTime(long.Parse(expire)))
		};

		return playerTokenData;
	}
}