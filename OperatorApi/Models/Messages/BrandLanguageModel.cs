using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class BrandLanguageModel
{
    [JsonPropertyName("culturecode")]
    public string CultureCode { get; set; }
}