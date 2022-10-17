using System.Linq;
using FluentValidation;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Core.Infrastructures.Extensions;
using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Validators;

public class GetLobbyGameUrlRequestValidator : AbstractValidator<GetLobbyGameUrlRequest>
{
    private readonly GameType[] _allowedGameTypes =
    {
        GameType.Sportsbook, GameType.VirtualSportsLobby, GameType.ESports,
        GameType.LiveDealerLobby, GameType.CardGameLobby, GameType.FishingLobby,
        GameType.ArcadeLobby, GameType.LotteryLobby
    };

    public GetLobbyGameUrlRequestValidator()
    {
		_ = RuleFor(x => x.SubGameProviderCode)
			.NotEmpty()
			.WithName(x => x
				.WithPropertyExpression(o => o.SubGameProviderCode)
				.GetBindPropertyName(nameof(x.SubGameProviderCode)));

		_ = RuleFor(x => x.GameType)
			.IsInEnum()
			.Must(gameType => _allowedGameTypes.Contains(gameType))
			.WithMessage(x => string.Format(
				"{0} must be in ({1})",
				x.WithPropertyExpression(o => o.GameType)
					.GetBindPropertyName(nameof(x.GameType)),
				_allowedGameTypes.Select(t => (int)t).JoinAsStringByComma()));

		_ = RuleFor(x => x.LanguageCode)
			.NotEmpty()
			.WithName(x => x
				.WithPropertyExpression(o => o.LanguageCode)
				.GetBindPropertyName(nameof(x.LanguageCode)));
    }
}
