using TGS.Core.Models.Enums;

namespace TGS.Services.Operator;

public record BrandAllAgentTurnover
{
	public int Rank { get; set; }
	public AllAgentGameType AllAgentGameType { get; set; }
	public decimal Turnover { get; set; }
	public int ActiveNumber { get; set; }
	public decimal RebatePercentage { get; set; }
}
