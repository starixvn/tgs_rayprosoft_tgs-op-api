using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TGS.Apis.OperatorApi.Models.Messages;

public record ChangePasswordRequest
{
    [Required]
    [JsonPropertyName("oldpwd")]
    public string OldPassword { get; set; }

    [Required]
    [JsonPropertyName("newpwd")]
    public string NewPassword { get; set; }
}