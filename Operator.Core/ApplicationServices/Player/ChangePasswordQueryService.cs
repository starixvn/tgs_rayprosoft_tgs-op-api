using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Core.Infrastructures.Security;
using Tgs.ServiceUtility;
using TGS.Core.Models;
using TGS.Core.Models.Exceptions;
using TGS.Services.Operations;
using TGS.Services.Operations.Models.Services;
using TGS.Services.Operations.Queries;
using TGS.Services.Operations.Validators;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class ChangePasswordQueryService : IGetService<ChangePasswordQuery, ChangePasswordInfo>
{
    private readonly IPasswordHandler _passwordHandler;
    private readonly IPlayerOperations _playerOperations;
    private readonly IPlayerQueries _playerQueries;
    private readonly IStatusValidator _statusValidator;

    public ChangePasswordQueryService(
        IStatusValidator statusValidator,
        IPlayerQueries playerQueries,
        IPasswordHandler passwordHandler,
        IPlayerOperations playerOperations)
    {
        _statusValidator = statusValidator ?? throw new ArgumentNullException(nameof(statusValidator));
        _playerQueries = playerQueries ?? throw new ArgumentNullException(nameof(playerQueries));
        _passwordHandler = passwordHandler ?? throw new ArgumentNullException(nameof(passwordHandler));
        _playerOperations = playerOperations ?? throw new ArgumentNullException(nameof(playerOperations));
    }

    public async ValueTask<ChangePasswordInfo> GetAsync(
        ChangePasswordQuery query, CancellationToken cancellationToken = default)
    {
        await _statusValidator.ThrowIfBrandStatusIsNotOneOfAsync(query.BrandId, ValidStatus.Default);

        var (salt, password) = await _playerQueries.GetSaltAndPasswordByPlayerCodeAsync(query.PlayerCode);

        if (!_passwordHandler.Verify(password, salt, query.OldPassword))
        {
            throw new InvalidCredentialsException("");
        }

        var username = await _playerOperations.GetUsernameByPlayerCodeAsync(query.PlayerCode);
        var updatedOn = DateTimeOffset.UtcNow;

        var (newSalt, newPassword) = _passwordHandler.GenerateCredential(query.NewPassword);

        await _playerOperations.ChangePlayerPasswordAsync(
            new ChangePlayerPasswordData
            {
                Username = username,
                PlayerCode = query.PlayerCode,
                Salt = newSalt,
                Password = newPassword,
                UpdatedOn = updatedOn
            });

        return new ChangePasswordInfo
        {
            PlayerCode = query.PlayerCode,
            UpdateTime = updatedOn
        };
    }
}
