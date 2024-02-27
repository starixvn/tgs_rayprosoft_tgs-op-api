namespace TGS.Apis.OperatorApi.Models;

public class PaymentFailException(string message) : Exception(message)
{
}