using TGS.Core.Models.Enums;

namespace TGS.Services.Operator.GameProviders
{
	public record BrandGameProviderInfo(
		string Code,
		string Name,
		string Description,
		bool IsTransferWallet,
		Status Status,
		string Localization);
}
