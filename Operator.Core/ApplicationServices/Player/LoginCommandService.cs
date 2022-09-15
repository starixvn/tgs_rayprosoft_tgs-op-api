using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgs.BrandCertificate;
using Tgs.BrandCertificate.Clients;
using Tgs.Core.Infrastructures.Security;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Core.Models.Exceptions;
using TGS.Core.Models.Tokens;
using TGS.Services.Operations;
using TGS.Services.Operations.Queries;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class LoginCommandService : IGetService<LoginCommand, LoginInfo>
{
    private readonly IBrandOperations _brandOperations;
    private readonly IBrandServiceClient _brandServiceClient;
    private readonly IHashProvider _hashProvider;
    private readonly IJwtTokenHandler _jwtTokenHandler;
    private readonly IPlayerLoginRecordRepository _loginRecordRepository;
    private readonly IPasswordHandler _passwordHandler;
    private readonly IPlayerQueries _playerQueries;
    private readonly IPlayerServiceClient _playerServiceClient;
    private readonly ITokenProvider _tokenProvider;

    public LoginCommandService(
        IPlayerQueries playerQueries,
        IPasswordHandler passwordHandler,
        ITokenProvider tokenProvider,
        IHashProvider hashProvider,
        IJwtTokenHandler jwtTokenHandler,
        IBrandOperations brandOperations,
        IPlayerLoginRecordRepository loginRecordRepository,
        IPlayerServiceClient playerServiceClient,
        IBrandServiceClient brandServiceClient)
    {
        _playerQueries = playerQueries ?? throw new ArgumentNullException(nameof(playerQueries));
        _passwordHandler = passwordHandler ?? throw new ArgumentNullException(nameof(passwordHandler));
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        _hashProvider = hashProvider ?? throw new ArgumentNullException(nameof(hashProvider));
        _jwtTokenHandler = jwtTokenHandler ?? throw new ArgumentNullException(nameof(jwtTokenHandler));
        _brandOperations = brandOperations ?? throw new ArgumentNullException(nameof(brandOperations));
        _loginRecordRepository = loginRecordRepository ?? throw new ArgumentNullException(nameof(loginRecordRepository));
        _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
        _brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
    }

    public async ValueTask<LoginInfo> GetAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var brandStatus = await _brandServiceClient
            .GetBrandStatusAsync(command.BrandId, cancellationToken)
            .ConfigureAwait(false);

        if (brandStatus != BrandStatus.Active)
            throw new InvalidStatusException($"brand with id '{command.BrandId}' is not active");

        var playerInfo = await _playerServiceClient
            .GetPlayerInfoByUserNameAsync(command.BrandId, command.UserName, cancellationToken)
            .ConfigureAwait(false);

        if (playerInfo is null)
        {
            throw new PlayerNotFoundException("");
        }

        var (salt, password) = await _playerQueries.GetSaltAndPasswordByPlayerCodeAsync(playerInfo.PlayerCode);

        if (!_passwordHandler.Verify(password, salt, command.Password))
        {
            throw new InvalidCredentialsException("");
        }

        var record = await _loginRecordRepository
            .AddPlayerLoginRecordAsync(
                command.BrandId,
                playerInfo.PlayerCode,
                command.PlatformType,
                command.IpAddress)
            .ConfigureAwait(false);

        var token = await GeneratePlayerJwtTokenAsync(
            command.BrandId,
            new PlayerTokenData
            {
                PlayerCode = playerInfo.PlayerCode,
                LoginRecordId = record.Id,
                PlatformType = record.PlatformType,
                ExpiredOn = record.ExpiredOn
            },
            command.Site);

        await _playerServiceClient
            .RecordLoginIpAsync(playerInfo.PlayerCode, command.IpAddress, command.HostName, cancellationToken)
            .ConfigureAwait(false);

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
