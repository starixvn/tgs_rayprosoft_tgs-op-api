using System;
using System.Threading.Tasks;
using TGS.Core.Infrastructures.Extensions;
using TGS.Core.Models;
using TGS.Services.Operations.CodeMappers;

namespace TGS.Services.Operations;

public interface IBrandGameProviderOperations
{
	Task<string> MapCultureAsync(Guid gameProviderId, Guid brandId, string cultureCode);
}

public sealed class BrandGameProviderOperations : IBrandGameProviderOperations
{
	private readonly IBrandOperations _brandOperations;
	private readonly IGameProviderOperations _gameProviderOperations;
	private readonly IGameProviderLanguageCodeMapper _languageCodeMapper;

	public BrandGameProviderOperations(
		IBrandOperations brandOperations,
		IGameProviderOperations gameProviderOperations,
		IGameProviderLanguageCodeMapper languageCodeMapper)
	{
		_brandOperations = brandOperations;
		_gameProviderOperations = gameProviderOperations;
		_languageCodeMapper = languageCodeMapper;
	}

	async Task<string> IBrandGameProviderOperations.MapCultureAsync(Guid gameProviderId, Guid brandId,
		string cultureCode)
	{
		var gameProviderLanguages = await _gameProviderOperations.GetGameProviderLanguagesAsync(gameProviderId);

		var internalCode = await GetSupportedLanguageAsync(gameProviderLanguages, cultureCode,
			() => _brandOperations.GetBrandGameProviderConfigurationDefaultLanguageAsync(brandId, gameProviderId));

		internalCode = await GetSupportedLanguageAsync(gameProviderLanguages, internalCode,
			() => Task.FromResult(Consts.DefaultCultureCode));

		return await _languageCodeMapper.InternalToExternalAsync(internalCode, gameProviderId);
	}

	private static async Task<string> GetSupportedLanguageAsync(string[] gameProviderLanguages,
		string language, Func<Task<string>> getDefaultLanguage)
	{
		return language.IsNotNullOrEmpty() && gameProviderLanguages.IgnoreCaseContains(language)
			? language
			: await getDefaultLanguage();
	}
}
