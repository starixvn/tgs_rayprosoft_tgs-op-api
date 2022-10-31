namespace TGS.Apis.OperatorApi.Models;

public class PaymentFailException : Exception
{
	public PaymentFailException(string message) : base(message)
	{
	}
}
