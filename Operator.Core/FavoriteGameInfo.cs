using TGS.Core.Models.Enums;

namespace TGS.Services.Operator;

public record FavoriteGameInfo(
	string PlayerCode,
	int GameId,
	GameType GameType,
	string SubGpCode);
