using System;

namespace TGS.Services.Operator.ApplicationServices.Game;

public record GetRecentBigWinQuery(
	Guid BrandId,
	int Top);
