using System.Threading.Tasks;

namespace TGS.Services.Operator;

public interface IAgentRepository
{
    ValueTask<AgentInfo?> GetAgentInfoByInvitationCodeAsync(string invitationCode);
}
