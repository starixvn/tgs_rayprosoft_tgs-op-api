using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgs.BrandCertificate.Clients;
using Tgs.Core.Infrastructures.Security;
using TGS.Core.Models.Enums;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Core.Models.Exceptions;
using TGS.Core.Models.Tokens;
using BrandStatus = Tgs.BrandCertificate.BrandStatus;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class LoginCommandService : IGetService<LoginCommand, LoginInfo>
{
    private readonly IBrandRepository _brandRepository;
    private readonly IBrandServiceClient _brandServiceClient;
    private readonly IJwtTokenHandler _jwtTokenHandler;
    private readonly IPlayerLoginRecordRepository _loginRecordRepository;
    private readonly IGetService<PlayerPasswordValidateCommand, bool> _passwordValidateCommandService;
    private readonly IPlayerServiceClient _playerServiceClient;

    public LoginCommandService(
        IJwtTokenHandler jwtTokenHandler,
        IBrandRepository brandRepository,
        IPlayerLoginRecordRepository loginRecordRepository,
        IPlayerServiceClient playerServiceClient,
        IBrandServiceClient brandServiceClient,
        IGetService<PlayerPasswordValidateCommand, bool> passwordValidateCommandService)
    {
        _jwtTokenHandler = jwtTokenHandler ?? throw new ArgumentNullException(nameof(jwtTokenHandler));
        _brandRepository = brandRepository ?? throw new ArgumentNullException(nameof(brandRepository));
        _loginRecordRepository = loginRecordRepository ?? throw new ArgumentNullException(nameof(loginRecordRepository));
        _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
        _brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
        _passwordValidateCommandService = passwordValidateCommandService ?? throw new ArgumentNullException(nameof(passwordValidateCommandService));
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

        var isValid = await _passwordValidateCommandService.GetAsync(
            new PlayerPasswordValidateCommand(playerInfo.PlayerCode, command.Password),
            cancellationToken)
            .ConfigureAwait(false);

        if (!isValid)
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
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, playerTokenData.PlayerCode),
                new Claim("login_id", playerTokenData.LoginRecordId.ToString()),
                new Claim("platform_type", PlatformType.Desktop.ToString())
            });

        var secretKey = await _brandRepository.GetSecretKeyAsync(brandId);
        var signature = Encoding.UTF8.GetBytes(secretKey!);

        return _jwtTokenHandler.GenerateToken(
            identity,
            signature,
            site,
            playerTokenData.ExpiredOn.DateTime);
    }
}
