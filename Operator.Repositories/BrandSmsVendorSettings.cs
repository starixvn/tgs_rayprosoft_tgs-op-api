using System;

namespace TGS.Operator.Repositories;

internal class BrandSmsVendorSettings
{
    public Uri DesktopLogoUrl { get; set; } = default!;

    public Uri LoadingUrl { get; set; } = default!;

    public Uri MobileLogoUrl { get; set; } = default!;
}
