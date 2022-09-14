using System;

namespace TGS.Services.Operator.BrandPayment;

public class InsufficientFundsException : Exception
{
    public InsufficientFundsException()
    {
    }
}
