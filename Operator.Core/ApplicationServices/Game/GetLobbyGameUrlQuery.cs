using System;
using System.Text.Json;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Tokens;

namespace TGS.Services.Operator.ApplicationServices.Game;

public record GetLobbyGameUrlQuery(
	Guid BrandId,
	Guid GameProviderId,
	PlayerTokenData PlayerTokenData,
	GameType GameType,
	string LanguageCode,
	string IpAddress,
	JsonDocument? Extends);
