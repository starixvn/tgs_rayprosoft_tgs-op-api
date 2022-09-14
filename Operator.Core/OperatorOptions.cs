using System;

namespace TGS.Services.Operator;

public class OperatorOptions
{
    public Uri CashBaseUrl { get; set; } = default!;

    public Uri FileServiceBaseUrl { get; set; } = new Uri("https://localhost:8001");

    public Uri SmsBaseUrl { get; set; } = new Uri("https://localhost:5002");

    public Uri ValidateCodeBaseUrl { get; set; } = new Uri("https://localhost:5001");
}
