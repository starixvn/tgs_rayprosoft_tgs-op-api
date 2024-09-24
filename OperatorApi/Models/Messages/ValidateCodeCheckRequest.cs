namespace TGS.Apis.OperatorApi.Models.Messages;

public class ValidateCodeCheckRequest
{
	public string SecurityKey { get; set; }
	public string ValidateCode { get; set; }
}

