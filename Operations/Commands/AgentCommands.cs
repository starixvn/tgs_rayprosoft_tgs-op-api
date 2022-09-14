using System;
using System.Threading.Tasks;
using Tgs.Logging;
using TGS.Core.DataServices.Tgs;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Core.Infrastructures.Extensions;
using TGS.Core.Infrastructures.Log;
using TGS.Core.Models.Enums;
using TGS.Services.Operations.Models.Services;

namespace TGS.Services.Operations.Commands;

public interface IAgentCommands
{
    Task<AgentLoginRecord> AgentLoginAsync(AgentLoginData data);

    Task ChangeAgentPasswordAsync(ChangeAgentPasswordData data);

    Task SetEmailAsync(Guid agentId, string email, DateTimeOffset updatedOn);
}

public sealed class AgentCommands : IAgentCommands
{
    private const int RetryCount = 3;
    private readonly ITgsLog _log;
    private readonly ITgsRepository _repository;

    public AgentCommands(
        ITgsRepository repository,
        ITgsLog log)
    {
        _repository = repository;
        _log = log;
    }

    public async Task<AgentLoginRecord> AgentLoginAsync(AgentLoginData data)
    {
        var loginRecord = new AgentLoginRecord
        {
            Id = GuidExtensions.NewSequentialGuid(),
            AgentId = data.AgentId,
            BrandId = data.BrandId,
            Status = LoginStatus.Enabled,
            IpAddress = data.IpAddress,
            CreatedOn = data.CreatedOn,
            ExpiredOn = data.ExpiredOn
        };

        await _repository.AgentLoginRecords.AddAsync(loginRecord);
        await _repository.SaveChangesAsync();

        return loginRecord;
    }

    public async Task ChangeAgentPasswordAsync(ChangeAgentPasswordData data)
    {
        var agent = await _repository.Agents.FindAsync(data.Id).ConfigureAwait(false);

        agent.Salt = data.Salt;
        agent.Password = data.Password;
        agent.UpdatedOn = data.UpdatedOn;

        await _repository.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task SetEmailAsync(Guid agentId, string email, DateTimeOffset updatedOn)
    {
        var agent = await _repository.Agents.FindAsync(agentId).ConfigureAwait(false);

        agent.Email = email;
        agent.UpdatedOn = updatedOn;

        await _repository.SaveChangesAsync().ConfigureAwait(false);
    }
}
