using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using TGS.Services.Operator.GameProviders;

namespace TGS.Services.Operator.ApplicationServices.GameProvider;
internal class GetGameProvidersService(
	IGameProviderRepository gameProviderRepository) : IGetService<GetGameProvidersQuery, IEnumerable<GameProviderInfo>>
{
	private readonly IGameProviderRepository _gameProviderRepository =
		gameProviderRepository ?? throw new ArgumentNullException(nameof(gameProviderRepository));

	public async ValueTask<IEnumerable<GameProviderInfo>> GetAsync(
		GetGameProvidersQuery query,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var brandId = query.BrandId;

		var playerTokenData = query.PlayerTokenData;
		var platformType = query.PlatformType;

		if (playerTokenData != null)
		{
			if (playerTokenData.PlatformType != platformType)
			{
				throw new InvalidOperationException("request platform is not matched with player login platform.");
			}
		}

		var brandGps = _gameProviderRepository.QueryAllGameProvidersAsync(brandId);

		return await brandGps.Select(gp => new GameProviderInfo
		{
			Code = gp.Code,
			Name = gp.Name,
			Description = gp.Description,
			Status = gp.Status,
			IsTransferWallet = gp.IsTransferWallet,
			Localization = string.IsNullOrEmpty(gp.Localization)
				? new Dictionary<string, JsonDocument>()
				: JsonSerializer.Deserialize<Dictionary<string, JsonDocument>>(
					gp.Localization)
		}).ToListAsync(cancellationToken);
	}
}
