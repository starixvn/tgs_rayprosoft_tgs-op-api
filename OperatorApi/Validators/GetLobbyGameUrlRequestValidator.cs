using FluentValidation;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Validators;

public class GetLobbyGameUrlRequestValidator : AbstractValidator<GetLobbyGameUrlRequest>
{
	private readonly GameType[] _allowedGameTypes = [
		GameType.Sportsbook,
		GameType.VirtualSportsLobby,
		GameType.ESports,
		GameType.LiveDealerLobby,
		GameType.CardGameLobby,
		GameType.FishingLobby,
		GameType.ArcadeLobby,
		GameType.LotteryLobby
	];

	public GetLobbyGameUrlRequestValidator()
	{
		_ = RuleFor(x => x.SubGameProviderCode)
			.NotEmpty();

		_ = RuleFor(x => x.GameType)
			.IsInEnum()
			.Must(gameType => _allowedGameTypes.Contains(gameType))
			.WithMessage($"value must be in {string.Join(",", _allowedGameTypes.Select(t => (int)t))}");

		_ = RuleFor(x => x.LanguageCode)
			.NotEmpty();
	}
}
