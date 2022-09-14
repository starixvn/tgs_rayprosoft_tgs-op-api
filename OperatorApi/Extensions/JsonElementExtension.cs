using System.Text.Json;

#nullable enable

namespace TGS.Apis.OperatorApi.Extensions;

public static class JsonElementExtenstion
{
    public static string? GetPropertyValue(this JsonElement jsonElement, string propertyName)
    {
        return jsonElement.TryGetProperty(propertyName, out var returnElement) ? returnElement.GetString() : null;
    }
}
