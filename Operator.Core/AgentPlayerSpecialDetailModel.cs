namespace TGS.Services.Operator;

public record AgentPlayerSpecialDetailModel
{
    public decimal Bonus { get; set; }
    public decimal BonusAdjust { get; set; }
    public decimal CompanyAdjust { get; set; }
    public decimal AgentAdjust { get; set; }
}