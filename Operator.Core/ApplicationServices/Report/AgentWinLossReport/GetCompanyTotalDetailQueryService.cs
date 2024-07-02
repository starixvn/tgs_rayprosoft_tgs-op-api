using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Report.Client;
using Tgs.Report.Client.Models.AgentWinLoss;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Report.AgentWinLossReport;

public class
	GetCompanyTotalDetailQueryService(
		IReportClient reportClient)
	: IGetService<GetCompanyTotalDetailQuery,
		IEnumerable<CompanyTotalDetailModel>>
{
	public async ValueTask<IEnumerable<CompanyTotalDetailModel>> GetAsync(
		GetCompanyTotalDetailQuery request,
		CancellationToken cancellationToken = default)
	{
		var response = await reportClient.GetCompanyTotalDetailAsync(new GetCompanyTotalDetailRequest
		{
			BrandId = request.BrandId,
			AgentId = request.AgentId,
			From = request.From,
			To = request.To
		}).ConfigureAwait(false);

		return response.Select(x => new CompanyTotalDetailModel
		{
			From = x.From,
			To = x.To,
			Percentage = x.Percentage,
			PlayerWinLoss = x.PlayerWinLoss,
			PlayerSpecial = x.PlayerSpecial,
			CompanyWinLoss = x.CompanyWinLoss,
			CompanySpecial = x.CompanySpecial,
			CompanyTotal = x.CompanyTotal
		});
	}
}