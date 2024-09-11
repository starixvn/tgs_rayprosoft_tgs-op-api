using TGS.Core.Models.Enums;

namespace TGS.Services.Operator;

public record FavoriteGameInfo(
	string PlayerCode,
	int GameId,
	Core.Models.Enums.GameType GameType,
	string SubGpCode);