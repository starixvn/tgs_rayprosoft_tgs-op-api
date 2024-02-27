using System;
using System.Text.Json;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

public record CreateBankInfoCommand(
	Guid BrandId,
	string PlayerCode,
	Guid PaymentChannelId,
	string BankType,
	JsonDocument Detail);