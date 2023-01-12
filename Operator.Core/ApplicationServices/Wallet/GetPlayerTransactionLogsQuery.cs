using System;

namespace TGS.Services.Operator.ApplicationServices.Wallet;
public record GetPlayerTransactionLogsQuery(
	string PlayerCode,
	DateTime From,
	DateTime To,
	int Page,
	int Rows);
