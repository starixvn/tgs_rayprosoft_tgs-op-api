using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TGS.Core.DataServices.Tgs;

namespace TGS.Services.Operations.Queries;

public interface ISettingQueries
{
    Task<string> GetSettingValueByKeyAsync(string key);
}

public sealed class SettingQueries : ISettingQueries
{
    private readonly ITgsRepository _repository;

    public SettingQueries(ITgsRepository repository)
    {
        _repository = repository;
    }

    Task<string> ISettingQueries.GetSettingValueByKeyAsync(string key)
    {
        return _repository.Settings
            .Where(x => x.Key == key)
            .Select(x => x.Value)
            .FirstOrDefaultAsync();
    }
}