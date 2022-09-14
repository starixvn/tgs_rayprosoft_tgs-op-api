using System;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator.ApplicationServices.Game;

public record ListFavoriteGameIdsQuery(Guid BrandId, string PlayerCode, GameType GameType);
