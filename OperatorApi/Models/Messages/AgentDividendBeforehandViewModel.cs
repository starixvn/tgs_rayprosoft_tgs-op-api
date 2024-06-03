using Tgs.Agent.Dividend.Client.Models;
using Tgs.BrandCertificate;

namespace TGS.Apis.OperatorApi.Models.Messages;

public class AgentDividendBeforehandViewModel
{
	public AgentDividendType AgentDividendType { get; set; }
	public IEnumerable<AgentDividendBeforehand> Previous { get; set; }
	public IEnumerable<AgentDividendBeforehand> Current { get; set; }
}