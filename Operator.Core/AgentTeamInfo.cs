namespace TGS.Services.Agent;
public class GetAgentTeamInfoModel
{
    public int DirectAgentCount { get; set; }
    public int IndirectAgentCount { get; set; }
    public int DirectPlayerCount { get; set; }
    public int IndirectPlayerCount { get; set; }
}