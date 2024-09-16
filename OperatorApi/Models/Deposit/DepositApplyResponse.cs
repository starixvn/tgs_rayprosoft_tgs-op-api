namespace TGS.Apis.OperatorApi.Models.Deposit;

public class DepositApplyResponse(string ThirdPartyOrderNo, string PaymentUrl)
{
	public string ThirdPartyOrderNo { get; set; } = ThirdPartyOrderNo;

	public string PaymentUrl { get; set; } = PaymentUrl;

	public double? ExpirationTime { get; set; }
}