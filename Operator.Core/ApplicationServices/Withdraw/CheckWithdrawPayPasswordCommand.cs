using System.Threading.Tasks;

namespace TGS.Services.Operator.ApplicationServices.Withdraw;

public record CheckWithdrawPayPasswordCommand(
	string PlayerCode,
	string PayPassword)
{
	public TaskCompletionSource<bool> ReturnSource { get; } = new TaskCompletionSource<bool>();
}