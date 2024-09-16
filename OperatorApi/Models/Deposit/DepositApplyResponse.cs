namespace TGS.Apis.OperatorApi.Models.Deposit;

public class DepositApplyResponse(string thirdPartyOrderNo, string paymentUrl)
{
	public string ThirdPartyOrderNo { get; set; } = thirdPartyOrderNo;

	public string PaymentUrl { get; set; } = paymentUrl;

	public double? ExpirationTime { get; set; }
}