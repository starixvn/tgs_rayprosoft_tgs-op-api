using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Report.Client;
using Tgs.Report.Client.Models.AgentWinLoss;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.AgentWinLossReport;

public class
    GetAgentTotalDetailQueryService(
        IReportClient reportClient)
    : IGetService<GetAgentTotalDetailQuery,
        IEnumerable<AgentTotalDetailModel>>
{
    public async ValueTask<IEnumerable<AgentTotalDetailModel>> GetAsync(
        GetAgentTotalDetailQuery request,
        CancellationToken cancellationToken = default)
    {
        var response = await reportClient.GetAgentTotalDetailAsync(new GetAgentTotalDetailRequest
        {
            BrandId = request.BrandId,
            AgentId = request.AgentId,
            From = request.From,
            To = request.To
        }).ConfigureAwait(false);

        return response.Select(x => new AgentTotalDetailModel
        {
            From = x.From,
            To = x.To,
            Percentage = x.Percentage,
            PlayerWinLoss = x.PlayerWinLoss,
            PlayerSpecial = x.PlayerSpecial,
            AgentWinLoss = x.AgentWinLoss,
            AgentSpecial = x.AgentSpecial,
            AgentTotal = x.AgentTotal
        });
    }
}