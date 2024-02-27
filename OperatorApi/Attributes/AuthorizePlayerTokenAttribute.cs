namespace TGS.Apis.OperatorApi.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class AuthorizePlayerTokenAttribute : Attribute
{
	public bool AllowNullOrEmpty { get; init; }

	public bool NeedAuthorize { get; init; }
}