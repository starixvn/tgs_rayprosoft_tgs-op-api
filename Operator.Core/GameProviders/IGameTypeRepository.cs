using System.Collections.Generic;
using System.Threading.Tasks;
using TGS.Core.Models.Enums;
using TGS.Services.Operator.ApplicationServices.GameTypes;

namespace TGS.Services.Operator.GameProviders;
public interface IGameTypeRepository
{

	Task<IEnumerable<GamesModel>> QueryGameByGameTypesAsync(GetGamesQuery query, GamePlatformType[] gamePlatformTypes);
}
