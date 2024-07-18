using System.Text.Json;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class DepositCashRegisterViewModel<TPayInfo>
{
	public Guid FormId { get; set; }

	public decimal Amount { get; set; }

	public DateTimeOffset? Expired { get; set; }

	public TPayInfo PayInfo { get; set; }
}