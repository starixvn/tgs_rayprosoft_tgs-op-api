using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Core.Infrastructures.Security;
using Tgs.ServiceUtility;
using TGS.Core.Infrastructures.Extensions;
using TGS.Core.Models;
using TGS.Core.Models.Enums;
using TGS.Core.Models.Exceptions;
using TGS.Core.Models.Tokens;
using TGS.Services.Operations;
using TGS.Services.Operations.Queries;
using TGS.Services.Operations.Validators;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class LoginCommandService : IGetService<LoginCommand, LoginInfo>
{
    private readonly IBrandOperations _brandOperations;
    private readonly IHashProvider _hashProvider;
    private readonly IJwtTokenHandler _jwtTokenHandler;
    private readonly IPlayerLoginRecordRepository _loginRecordRepository;
    private readonly IPasswordHandler _passwordHandler;
    private readonly IPlayerOperations _playerOperations;
    private readonly IPlayerQueries _playerQueries;
    private readonly IPlayerRepository _playerRepository;
    private readonly IStatusValidator _statusValidator;
    private readonly ITokenProvider _tokenProvider;

    public LoginCommandService(
        IStatusValidator statusValidator,
        IPlayerOperations playerOperations,
        IPlayerQueries playerQueries,
        IPasswordHandler passwordHandler,
        ITokenProvider tokenProvider,
        IHashProvider hashProvider,
        IJwtTokenHandler jwtTokenHandler,
        IBrandOperations brandOperations,
        IPlayerRepository playerRepository,
        IPlayerLoginRecordRepository loginRecordRepository)
    {
        _statusValidator = statusValidator ?? throw new ArgumentNullException(nameof(statusValidator));
        _playerOperations = playerOperations ?? throw new ArgumentNullException(nameof(playerOperations));
        _playerQueries = playerQueries ?? throw new ArgumentNullException(nameof(playerQueries));
        _passwordHandler = passwordHandler ?? throw new ArgumentNullException(nameof(passwordHandler));
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        _hashProvider = hashProvider ?? throw new ArgumentNullException(nameof(hashProvider));
        _jwtTokenHandler = jwtTokenHandler ?? throw new ArgumentNullException(nameof(jwtTokenHandler));
        _brandOperations = brandOperations ?? throw new ArgumentNullException(nameof(brandOperations));
        _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
        _loginRecordRepository = loginRecordRepository ?? throw new ArgumentNullException(nameof(loginRecordRepository));
    }

    public async ValueTask<LoginInfo> GetAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        await _statusValidator.ThrowIfBrandStatusIsNotOneOfAsync(command.BrandId, ValidStatus.Default);

        var playerCode = await _playerOperations.GetPlayerCodeByUsernameAsync(command.BrandId, command.UserName);

        if (playerCode.IsNullOrEmpty())
        {
            throw new PlayerNotFoundException("");
        }

        var (salt, password) = await _playerQueries.GetSaltAndPasswordByPlayerCodeAsync(playerCode);

        if (!_passwordHandler.Verify(password, salt, command.Password))
        {
            throw new InvalidCredentialsException("");
        }

        _statusValidator.ThrowIfPlayerDisabled(PlayerStatus.Enabled);

        var record = await _loginRecordRepository.AddPlayerLoginRecordAsync(command.BrandId, playerCode, command.PlatformType, command.IpAddress).ConfigureAwait(false);

        var token = await GeneratePlayerJwtTokenAsync(
            command.BrandId,
            new PlayerTokenData
            {
                PlayerCode = playerCode,
                LoginRecordId = record.Id,
                PlatformType = record.PlatformType,
                ExpiredOn = record.ExpiredOn
            },
            command.Site);

        await _playerRepository.PlayerLoginIpAsync(playerCode, command.IpAddress, command.HostName);

        return new LoginInfo
        {
            PlayerToken = token,
            LoginTime = record.CreatedOn.ToUnixTimeMilliseconds()
        };
    }

    private async Task<string> GeneratePlayerJwtTokenAsync(
        Guid brandId, PlayerTokenData playerTokenData, string site)
    {
        var data = await _tokenProvider.EncryptAsync(playerTokenData);

        var jti = Guid.NewGuid().ToString();

        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, data),
                new Claim(JwtRegisteredClaimNames.Jti, jti)
            });

        var payload = _hashProvider.Hash($"{data}:{jti}");
        var secretKey = await _brandOperations.GetBrandSecretKeyAsync(brandId);
        var signature = Encoding.UTF8.GetBytes($"{payload}:{secretKey}");

        return _jwtTokenHandler.GenerateToken(
            identity,
            signature,
            site,
            playerTokenData.ExpiredOn.DateTime);
    }
}
