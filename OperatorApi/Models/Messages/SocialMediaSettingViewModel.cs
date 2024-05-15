using Tgs.BrandCertificate;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class SocialMediaSettingViewModel
{
	public required Guid Id { get; set; }

	public required string Name { get; set; }

	public required SocialType Type { get; set; }

	public required Uri Url { get; set; }
}
