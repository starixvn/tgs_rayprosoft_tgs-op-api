namespace TGS.Apis.OperatorApi.Models;

public class UmPayApiException : Exception
{
	public UmPayApiException()
		: base($"UmPay api has error.")
	{
	}
}
