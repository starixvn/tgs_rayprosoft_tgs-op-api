using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TGS.Core.DataServices.Base.Extensions;
using TGS.Core.DataServices.Tgs;
using TGS.Core.Models.Services.Data;

namespace TGS.Services.Operations.Queries;

public interface ILocalizationStringQueries
{
    Task<LocalizedStringResult[]> GetLocalizationByGameProviderIdAsync(Guid gameProviderId);
}

public sealed class LocalizationStringQueries : ILocalizationStringQueries
{
    private readonly ITgsRepository _repository;

    public LocalizationStringQueries(ITgsRepository repository)
    {
        _repository = repository;
    }

    async Task<LocalizedStringResult[]> ILocalizationStringQueries.GetLocalizationByGameProviderIdAsync(
        Guid gameProviderId)
    {
        const string spName = "dbo.localization_get_localization_by_game_provider_id";
        var r = await _repository.SqlQueryMultipleAsync<LocalizedStringResult>(spName,
            parameters: new Dictionary<string, object>
            {
                {":game_provider_id", gameProviderId}
            });

        return r.ToArray();
    }
}