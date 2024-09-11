using System;

namespace TGS.Services.Operator.ApplicationServices.Game;

public record ListFavoriteGameIdsQuery(Guid BrandId, string PlayerCode, Core.Models.Enums.GameType GameType);