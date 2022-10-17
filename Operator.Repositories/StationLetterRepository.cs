using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TGS.Core.DataServices.Tgs;
using TGS.Core.Models.Enums;
using Tgs.Paging;
using TGS.Services.Operator.StationLetters;

namespace TGS.Operator.Repositories;

internal class StationLetterRepository : IStationLetterRepository
{
    private readonly ITgsRepository _tgsRepository;

    public StationLetterRepository(ITgsRepository tgsRepository)
    {
        _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
    }

    public IAsyncEnumerable<StationLetter> QueryStationLettersAsync(string playerCode, Paging<StationLetter> paging)
    {
        var query = QueryStationLettersAsync(playerCode)
            .ApplyOrderBy(paging)
            .ApplyPaging(paging);

        return query;
    }

    public IAsyncEnumerable<StationLetter> QueryStationLettersAsync(string playerCode)
        => InternalQueryStationLetters(playerCode)
            .ToAsyncEnumerable()
            .Select(letterPlayer => new StationLetter(
                letterPlayer.StationLetterId,
                letterPlayer.StationLetter.Subject,
                letterPlayer.StationLetter.Content,
                letterPlayer.StationLetter.CreatedOn,
                letterPlayer.IsRead,
                letterPlayer.StationLetter.Localization == null
                    ? null
                    : JsonSerializer.Deserialize<IDictionary<string, JsonDocument>>(
                        letterPlayer.StationLetter.Localization)));

    public ValueTask<int> QueryStationLettersTotalCountAsync(string playerCode)
        => new ValueTask<int>(InternalQueryStationLetters(playerCode).CountAsync());

    public async ValueTask SetStationLetterHaveReadAsync(string playerCode, Guid letterId)
    {
        var letter = await _tgsRepository.StationLetterPlayers
            .FindAsync(letterId, playerCode)
            .ConfigureAwait(false);

        if (letter is not null)
        {
            letter.IsRead = true;
            letter.ReadOn = DateTimeOffset.UtcNow;

			_ = await _tgsRepository.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    private IQueryable<Core.DataServices.Tgs.Models.StationLetterPlayer> InternalQueryStationLetters(string playerCode)
        => from letterPlayer in _tgsRepository.StationLetterPlayers.AsQueryable()
            .Include(letterPlayer => letterPlayer.StationLetter)
           where letterPlayer.PlayerCode == playerCode
            && letterPlayer.StationLetter.Status == StationLetterStatus.Published
           orderby letterPlayer.StationLetter.CreatedOn descending
           select letterPlayer;
}
