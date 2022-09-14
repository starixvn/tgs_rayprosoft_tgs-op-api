using System;

namespace TGS.Services.Operator.ApplicationServices.Deposit;

public record GetDepositFormQuery(string PlayerCode, Guid FormId, Guid BrandId);
