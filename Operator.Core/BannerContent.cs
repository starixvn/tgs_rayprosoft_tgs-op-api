using System;

namespace TGS.Services.Operator;

public record BannerContent(
	string Title,
	Uri DesktopBannerUrl,
	Uri MobileBannerUrl,
	Uri NavigateUrl,
	string PictureAlt);