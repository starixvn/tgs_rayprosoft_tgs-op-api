using System;
using System.IO;

namespace TGS.Services.Operator.ApplicationServices.Deposit;

public record SubmitBankDepositFormCommand(
	Guid BrandId,
	string PlayerCode,
	Guid FormId,
	string ContentType,
	Stream FileStream,
	Func<string, string> CreatePaymentReturn);