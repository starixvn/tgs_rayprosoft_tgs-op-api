using System;
using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record GetPlayerPromotionRecordRequest : PagingRequest
{
    [JsonPropertyName("from")]
    public DateTimeOffset From { get; set; }

    [JsonPropertyName("to")]
    public DateTimeOffset To { get; set; }
}