using System;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

public record DeleteBankInfoCommand(
	string PlayerCode,
	Guid BankInfoId);
