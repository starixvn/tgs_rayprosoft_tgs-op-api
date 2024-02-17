using System.Threading;
using System.Threading.Tasks;
using Tgs.ServiceUtility;
using Tgs.VipLevelService.Clients;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class GetPlayerVipLevelQueryService(
	IVipLevelServiceClient vipLevelServiceClient) : IGetService<GetPlayerVipLevelQuery, Tgs.VipLevelService.Common.ViewModels.PlayerVipLevelInfoViewModel>
{
	public ValueTask<Tgs.VipLevelService.Common.ViewModels.PlayerVipLevelInfoViewModel> GetAsync(GetPlayerVipLevelQuery query, CancellationToken cancellationToken = default)
		=> vipLevelServiceClient.PlayerVipLevelGetAsync(query.PlayerCode, cancellationToken);

}
