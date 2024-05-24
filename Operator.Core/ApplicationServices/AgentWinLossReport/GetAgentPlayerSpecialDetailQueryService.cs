using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Report.Client;
using Tgs.Report.Client.Models.AgentWinLoss;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.AgentWinLossReport;

public class
    GetAgentPlayerSpecialDetailQueryService(IReportClient reportClient) : IGetService<GetAgentPlayerSpecialDetailQuery,
    AgentPlayerSpecialDetailModel>
{
    public async ValueTask<AgentPlayerSpecialDetailModel> GetAsync(
        GetAgentPlayerSpecialDetailQuery request,
        CancellationToken cancellationToken = default)
    {
        var response = await reportClient.GetAgentPlayerSpecialDetailAsync(new GetAgentPlayerSpecialDetailRequest
        {
            BrandId = request.BrandId, AgentId = request.AgentId, From = request.From, To = request.To
        }).ConfigureAwait(false);

        return new AgentPlayerSpecialDetailModel
        {
            Bonus = response.Bonus,
            BonusAdjust = response.BonusAdjust
        };
    }
}