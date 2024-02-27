using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using TGS.Apis.OperatorApi.Attributes;

namespace TGS.Apis.OperatorApi.Models.Messages;

[ModelBinder(typeof(JsonWithFilesFormDataModelBinder), Name = "request")]
public record CreateIdentityAuthenticationFormRequest
{
	[Required]
	[JsonPropertyName("realname")]
	public string RealName { get; init; }

	[Required]
	[JsonPropertyName("id")]
	public string Identity { get; init; }
}