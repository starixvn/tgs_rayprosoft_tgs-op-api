#nullable enable

using TGS.Core.Models.Enums;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class DirectPlayersResponse
{
	public string PlayerUsername { get; set; } = string.Empty;
	public int VipLevel { get; set; }
	public DateTime? RegisteredOn { get; set; }
	public DateTime? LastLoginOn { get; set; }
	public decimal TotalDepositAmount { get; set; }
	public decimal TotalWithdrawAmount { get; set; }
	public PlayerStatus PlayerStatus { get; set; }
}