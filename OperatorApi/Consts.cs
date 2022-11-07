namespace TGS.Apis.OperatorApi;

internal class Consts
{
	public const string CsrfTokenName = "X-CSRF-TOKEN";

	public const string DefaultCulture = "zh-CN";
	public const string DefaultCurrency = "CNY";

	public const string Site = "operator-api";

	public static readonly string[] DefaultBypassRouting = { "/health", "/metrics", "/favicon.ico", "/swagger", "/profiler" };
}
