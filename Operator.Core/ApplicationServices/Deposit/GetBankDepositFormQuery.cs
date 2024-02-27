using System;

namespace TGS.Services.Operator.ApplicationServices.Deposit;

public record GetBankDepositFormQuery(string PlayerCode, Guid FormId, Guid BrandId);