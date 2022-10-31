#nullable enable

namespace TGS.Apis.OperatorApi.Models.Messages;

public class AgentInvitationInfoResponse
{
	public Guid AgentId { get; set; }

	public string InvitationCode { get; set; } = default!;

	public string UserName { get; set; } = default!;
}
