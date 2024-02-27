using System;
using TGS.Core.Models.Tokens;

namespace TGS.Services.Operator.ApplicationServices.Game;

public record GetBetConstructHtmlContentQuery(PlayerTokenData PlayerTokenData, Guid BrandId, string IP, string CultureName);