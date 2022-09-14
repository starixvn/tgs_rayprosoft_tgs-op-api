using System;
using System.Text.Json;
using TGS.Core.Models.Tokens;

namespace TGS.Services.Operator.ApplicationServices.Game;

public record GetGameUrlQuery(
    Guid BrandId,
    PlayerTokenData PlayerTokenData,
    int GameId,
    string LanguageCode,
    string IpAddress,
    JsonDocument? ExtendedData);
