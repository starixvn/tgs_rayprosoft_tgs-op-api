using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Tgs.BrandCertificate.Clients;

#nullable enable

namespace TGS.Services.Operator.PlayerRegisterFlow;

internal class BrandFlowDefinitionManager(
	RegisterFlowBuilder registerFlowBuilder,
	IBrandServiceClient brandServiceClient,
	IMemoryCache memoryCache) : IBrandRegisterFlowDefinitionManager, IBrandFrontendInfoCacheCleaner
{
	private readonly IBrandServiceClient _brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
	private readonly IMemoryCache _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
	private readonly RegisterFlowBuilder _registerFlowBuilder = registerFlowBuilder ?? throw new ArgumentNullException(nameof(registerFlowBuilder));

	public void Clean(Guid brandId)
	{
		var key = $"Flow:{brandId}";

		_memoryCache.Remove(key);
	}

	public async Task<RegisterFlowDefinition> GetRegisterFlowDefinitionAsync(Guid brandId, CancellationToken cancellationToken = default)
	{
		var key = $"Flow:{brandId}";

		var settings = await _memoryCache.GetOrCreateAsync(
			key,
			async entry =>
			{
				var brandFrontendInfo = await _brandServiceClient
					.GetBrandFrontendInfoAsync(brandId)
					.ConfigureAwait(false);

				entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8D);

				return brandFrontendInfo?.PlayerRegisterFlowSettings;
			}).ConfigureAwait(false);

		return _registerFlowBuilder.LoadSettings(settings ?? JsonDocument.Parse("[]"));
	}
}
