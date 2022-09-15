#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TGS.Core.DataServices.Tgs;
using TGS.Core.Models.Enums;
using TGS.Services.Operator;

namespace TGS.Operator.Repositories;

internal class BrandRepository : IBrandRepository
{
    private readonly ISystemClock _systemClock;
    private readonly ITgsRepository _tgsRepository;

    public BrandRepository(ITgsRepository tgsRepository, ISystemClock systemClock)
    {
        _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
        _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
    }

    public async ValueTask<string?> GetSecretKeyAsync(
        Guid brandId,
        CancellationToken cancellationToken = default)
    {
        return await _tgsRepository.Brands
            .Where(b => b.Id == brandId)
            .Select(b => b.SecretKey)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public IAsyncEnumerable<AnnouncementInfo> QueryAnnouncementInfosAsync(Guid brandId, DateTimeOffset searchDate)
    {
        var now = DateTimeOffset.UtcNow;

        return _tgsRepository.Announcements.AsQueryable()
            .Where(a => a.BrandId == brandId
                && a.Status == AnnounceStatus.Effective
                && searchDate >= a.From
                && searchDate < a.To)
            .Select(a => new
            {
                a.Subject,
                a.Content,
                a.From,
                a.To,
                a.Localization,
                a.DisplayOrder,
                a.Type,
                a.CreatedOn
            })
            .ToAsyncEnumerable()
            .Select(a => new AnnouncementInfo(
                a.Subject,
                a.Content,
                a.From,
                a.To,
                a.DisplayOrder,
                a.Localization == null
                    ? null
                    : JsonSerializer.Deserialize<IDictionary<string, JsonDocument>>(
                        a.Localization),
                a.Type,
                a.CreatedOn));
    }

    public IAsyncEnumerable<BannerInfo> QueryBrandBannersAsync(Guid brandId)
    {
        var currentTime = _systemClock.UtcNow;

        var rows = from banner in _tgsRepository.Banners.AsQueryable()
                   where banner.Status == 1000
                       && banner.BrandId == brandId
                       && banner.BeginTime <= currentTime
                       && (banner.EndTime == null || banner.EndTime > currentTime)
                       && banner.IsEnabled
                   orderby banner.DisplayOrder
                   select new BannerInfo(
                       banner.Id,
                       banner.Title,
                       banner.Localization);

        return rows.ToAsyncEnumerable();
    }
}
