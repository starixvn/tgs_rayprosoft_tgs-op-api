#nullable enable

using TGS.Core.Models.Tokens;
using static TGS.Core.Models.Consts.ContextItems;

namespace TGS.Apis.OperatorApi.Extensions;

internal static class ContextItemExtensions
{
	public static Guid GetBrandId(this IDictionary<object, object?> items) =>
		items.TryGetValue(BrandId, out var v) && v != null ? (Guid)v : Guid.Empty;

	public static PlayerTokenData? GetPlayerTokenData(this IDictionary<object, object?> items) =>
		items.TryGetValue(PlayerToken, out var v) && v is not null ? (PlayerTokenData)v : null;
}