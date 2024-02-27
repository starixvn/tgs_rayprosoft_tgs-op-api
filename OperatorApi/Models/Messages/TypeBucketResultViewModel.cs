#nullable enable

using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class TypeBucketResultViewModel
{
	[JsonPropertyName("amount")]
	public decimal Amount { get; set; }

	[JsonPropertyName("buckettype")]
	public string? BucketType { get; set; }
}