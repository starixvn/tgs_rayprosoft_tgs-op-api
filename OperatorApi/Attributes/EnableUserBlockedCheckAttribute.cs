namespace TGS.Apis.OperatorApi.Attributes;

/// <summary>
/// When player status is disabled, player can't use this api
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class EnableUserBlockedCheckAttribute : Attribute
{
}