using System;

namespace TGS.Services.Operator.ApplicationServices.Game;

public record SetFavoriteGameCommand(
    Guid BrandId,
    string PlayerCode,
    int GameId);
