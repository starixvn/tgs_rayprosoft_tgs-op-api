namespace TGS.Apis.OperatorApi.Models.Messages;

public record ForgotPasswordRequest
{
	public string Email { get; set; }

	public string AreaCode { get; set; }

	public string Phone { get; set; }
}