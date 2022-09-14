using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace TGS.Apis.OperatorApi.Models.Messages;

[BindProperties(SupportsGet = true)]
public record PagingRequest
{
    [JsonPropertyName("page")]
    [FromQuery(Name = "page")]
    public int Page { get; init; }

    [JsonPropertyName("rows")]
    [FromQuery(Name = "rows")]
    public int Rows { get; init; }

    [JsonPropertyName("desc")]
    [FromQuery(Name = "desc")]
    public bool IsDesc { get; init; }

    [JsonPropertyName("sort")]
    [FromQuery(Name = "sort")]
    public string SortBy { get; init; }

    [JsonPropertyName("all")]
    [FromQuery(Name = "all")]
    public bool ShowAll { get; init; }
}
