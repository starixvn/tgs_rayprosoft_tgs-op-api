using System;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

public record CreateWithdrawFormCommand(
	string PlayerCode,
	Guid BrandId,
	Guid BankInfoId,
	decimal Amount,
	string? Note);
