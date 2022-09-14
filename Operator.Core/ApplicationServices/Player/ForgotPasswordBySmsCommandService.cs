using System;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Tgs.BrandCertificate.Clients;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Services.Operator.Properties;
using TGS.Services.Operator.ValidateCode;
using TGS.SmsService.Client;

namespace TGS.Services.Operator.ApplicationServices.Player;

internal class ForgotPasswordBySmsCommandService : IGetService<ForgotPasswordBySmsCommand, string>
{
    private readonly IBrandServiceClient _brandServiceClient;
    private readonly IValidateCodeGenerator _codeGenerator;
    private readonly IPlayerServiceClient _playerServiceClient;
    private readonly ISmsServiceClient _smsServiceClient;
    private readonly IValidateCodeOperator _validateCodeOperator;

    public ForgotPasswordBySmsCommandService(
        IPlayerServiceClient playerServiceClient,
        IValidateCodeGenerator codeGenerator,
        IValidateCodeOperator validateCodeOperator,
        IBrandServiceClient brandServiceClient,
        ISmsServiceClient smsServiceClient)
    {
        _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
        _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
        _validateCodeOperator = validateCodeOperator ?? throw new ArgumentNullException(nameof(validateCodeOperator));
        _brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
        _smsServiceClient = smsServiceClient ?? throw new ArgumentNullException(nameof(smsServiceClient));
    }

    public async ValueTask<string> GetAsync(ForgotPasswordBySmsCommand command, CancellationToken cancellationToken = default)
    {
        var player = await _playerServiceClient
            .GetPlayerInfoByPhoneAsync(command.BrandId, command.AreaCode, command.Phone)
            .ConfigureAwait(false);

        if (player is null)
            throw new PlayerNotFoundException($"The phone ({command.Phone}) can't find the player info.");

        var saveResult = await Policy.HandleResult<(bool Result, string ValidateCode, string Token)>(data => !data.Result)
            .RetryAsync(5)
            .ExecuteAsync(async () =>
            {
                var validateCode = _codeGenerator.GenerateCode();
                var token = _codeGenerator.GenerateSecurityKey();

                var data = new ValidateCodeData(player.PlayerCode, token);
                
                var result = await _validateCodeOperator
                    .SaveValidateDataAsync(validateCode, token, data)
                    .ConfigureAwait(false);

                return (result, validateCode, token);
            })
            .ConfigureAwait(false);

        if (!saveResult.Result)
            throw new GenerateValidateCodeFailException();

        var brandInfo = await _brandServiceClient
            .GetBrandInfoAsync(command.BrandId)
            .ConfigureAwait(false);

        if (brandInfo?.SmsEnabled != true || !player.PhoneAuthentication)
            throw new PlayerPhoneNotSetException();

        await _smsServiceClient.SendShortMessageAsync(
            $"{command.AreaCode}{command.Phone}",
            string.Format(
                Resources.ResourceManager.GetString(nameof(Resources.ValidateCodeMailBody), command.CultureInfo)!,
                saveResult.ValidateCode),
            player.BrandId).ConfigureAwait(false);

        return saveResult.Token;
    }
}
