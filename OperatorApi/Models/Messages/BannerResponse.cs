using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class BannerResponse
{
	[JsonPropertyName("desktopurl")]
	public Uri DesktopBannerUrl { get; set; }

	[JsonPropertyName("id")]
	public Guid Id { get; set; }

	[JsonPropertyName("mobileurl")]
	public Uri MobileBannerUrl { get; set; }

	[JsonPropertyName("navurl")]
	public Uri NavigateUrl { get; set; }

	[JsonPropertyName("alt")]
	public string PictureAlt { get; set; }

	[JsonPropertyName("title")]
	public string Title { get; set; }
}
