using System.Collections.Generic;
using System.Threading.Tasks;
using TGS.Core.Models.Enums;
using TGS.Services.Operator.ApplicationServices.Game;
using GetGamesQuery = TGS.Services.Operator.ApplicationServices.GameTypes.GetGamesQuery;

namespace TGS.Services.Operator.GameProviders;
public interface IGamesRepository
{

	Task<IEnumerable<GamesModel>> QueryGameByGameTypesAsync(GetGamesQuery query, GamePlatformType[] gamePlatformTypes);

	Task<IEnumerable<GameTagInfo>> QueryGameByTagsAsync(GetTopGamesByTagQuery query, GamePlatformType[] gamePlatformTypes);
}
