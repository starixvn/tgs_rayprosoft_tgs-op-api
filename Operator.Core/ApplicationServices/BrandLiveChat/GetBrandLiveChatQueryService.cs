using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tgs.ServiceUtility;
using TGS.Core.DataServices.Tgs;

namespace TGS.Services.Operator.ApplicationServices.BrandLiveChat;

internal class GetBrandLiveChatQueryService : IGetService<BrandLiveChatQuery, BrandLiveChatInfo?>
{
    private readonly ITgsRepository _tgsRepository;

    public GetBrandLiveChatQueryService(ITgsRepository tgsRepository)
    {
        _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
    }

    public async ValueTask<BrandLiveChatInfo?> GetAsync(
        BrandLiveChatQuery query,
        CancellationToken cancellationToken = default)
    {
        var brandLiveChat = await _tgsRepository.BrandLiveChats
            .FirstOrDefaultAsync(x => x.BrandId == query.BrandId);
        if (brandLiveChat == null)
        {
            return null;
        }

        return new BrandLiveChatInfo()
        {
            Name = brandLiveChat.ChatName,
            Setting = brandLiveChat.ChatSetting
        };
    }
}
