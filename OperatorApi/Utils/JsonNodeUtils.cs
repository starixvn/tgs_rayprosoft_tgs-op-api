using System.Text.Json.Nodes;

namespace TGS.Apis.OperatorApi.Utils;

public static class JsonNodeExtensions
{
	public static T GetValueOrThrow<T>(this JsonNode jsonNode, string key)
		=> jsonNode[key] == null ? throw new ArgumentException($"Missing required field: {key}") : jsonNode[key]!.GetValue<T>();

	public static T GetValueOrDefault<T>(this JsonNode jsonNode, string key, T defaultValue)
		=> jsonNode[key] == null ? defaultValue : jsonNode[key]!.GetValue<T>();
}