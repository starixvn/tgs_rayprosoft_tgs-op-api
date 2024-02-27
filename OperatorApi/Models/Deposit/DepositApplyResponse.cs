namespace TGS.Apis.OperatorApi.Models.Deposit;

public record struct DepositApplyResponse(
	string ThirdPartyOrderNo,
	string PaymentUrl);