using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class PagingResponse<T>
{
    [JsonPropertyName("totalcount")]
    public int TotalCount { get; init; }

    [JsonPropertyName("items")]
    public IEnumerable<T> Items { get; init; }
}
