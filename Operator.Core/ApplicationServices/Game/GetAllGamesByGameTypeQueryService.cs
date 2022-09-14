using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using Tgs.ServiceUtility;
using TGS.Core.Models;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Extensions;
using TGS.Services.Operator.GameProviders;

namespace TGS.Services.Operator.ApplicationServices.Game;

internal class GetAllGamesByGameTypeQueryService : IQueryService<GetAllGamesByGameTypeQuery, GameInfo>
{
    private readonly IGameProviderRepository _gameProviderRepository;
    private readonly IValidStatus<GameStatus> _validGameStatus;
    private readonly IValidStatus<Status> _validStatus;

    public GetAllGamesByGameTypeQueryService(
        IValidStatus<Status> validStatus,
        IValidStatus<GameStatus> validGameStatus,
        IGameProviderRepository gameProviderRepository)
    {
        _validStatus = validStatus ?? throw new ArgumentNullException(nameof(validStatus));
        _validGameStatus = validGameStatus ?? throw new ArgumentNullException(nameof(validGameStatus));

        _gameProviderRepository =
            gameProviderRepository ?? throw new ArgumentNullException(nameof(gameProviderRepository));
    }

    public async IAsyncEnumerable<GameInfo> QueryAsync(
        GetAllGamesByGameTypeQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var gamePlatformTypes = query.PlatformType.ToGamePlatformTypes();
        var statuses = _validStatus.GetStatus(query.PlayerType);
        var gameStatuses = _validGameStatus.GetStatus(query.PlayerType);

        var brandSubGps = await _gameProviderRepository.QueryAllSubGameProvidersAsync(query.BrandId)
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);

        var brandGames = await _gameProviderRepository.QueryAllGamesAsync(query.BrandId)
            .Where(g => gamePlatformTypes.Contains(g.PlatformType))
            .Where(g => g.AvailableToBrand)
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);

        var gameTypes = query.Types;

        var infos = from g in brandGames
                    join sgp in brandSubGps on g.SubGpCode equals sgp.Code
                    where gameTypes.Length == 0
                        || gameTypes.Contains(g.Type)
                    let isActive = statuses.Contains(sgp.Status)
                        && gameStatuses.Contains(g.Status)
                    select new
                    {
                        g.Id,
                        g.Code,
                        g.Name,
                        g.Description,
                        GameProviderCode = g.SubGpCode,
                        g.Type,
                        IsActive = isActive,
                        g.Tag,
                        g.DisplayOrder,
                        g.Localization
                    };

        foreach (var item in infos.OrderBy(g => g.Id))
        {
            yield return new GameInfo
            {
                Id = item.Id,
                Code = item.Code,
                Name = item.Name,
                Description = item.Description,
                GameProviderCode = item.GameProviderCode,
                Type = item.Type,
                IsActive = item.IsActive,
                Tag = new TagInfo
                {
                    New = item.Tag.IsNew(),
                    Hot = item.Tag.IsHot(),
                    Recommend = item.Tag.IsRecommend()
                },
                DisplayOrder = item.DisplayOrder,
                Localization = string.IsNullOrEmpty(item.Localization)
                    ? new Dictionary<string, JsonDocument>()
                    : JsonSerializer.Deserialize<Dictionary<string, JsonDocument>>(
                        item.Localization)
            };

            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }
        }
    }
}
