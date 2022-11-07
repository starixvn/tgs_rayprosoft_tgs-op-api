using System;
using System.Threading.Tasks;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

public record CreateWithdrawFormCommand(
	string PlayerCode,
	Guid BrandId,
	Guid BankInfoId,
	decimal Amount,
	string? Note)
{
	public TaskCompletionSource<Guid> ReturnFormIdSource { get; } = new TaskCompletionSource<Guid>();
}
