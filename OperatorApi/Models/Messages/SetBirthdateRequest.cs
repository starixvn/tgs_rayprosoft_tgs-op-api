using System;
using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class SetBirthdateRequest
{
    [JsonPropertyName("birthdate")]
    public DateOnly Birthdate { get; set; }
}
