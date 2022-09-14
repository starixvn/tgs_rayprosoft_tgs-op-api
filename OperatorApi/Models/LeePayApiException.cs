using System;

namespace TGS.Apis.OperatorApi.Models;

public class LeePayApiException : Exception
{
    public LeePayApiException()
        : base($"LeePay api has error.")
    {
    }
}