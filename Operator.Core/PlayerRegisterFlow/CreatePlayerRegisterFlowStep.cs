using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Tgs.BrandCertificate.Clients;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Services.Operator.ApplicationServices.Player;

namespace TGS.Services.Operator.PlayerRegisterFlow;

internal class CreatePlayerRegisterFlowStep : IRegisterFlowStep
{
    private readonly IBrandServiceClient _brandServiceClient;
    private readonly IGetService<CreatePlayerWithAgentInvitationCodeCommand, CreatePlayerInfo> _createAgentInvitationPlayerService;
    private readonly IGetService<CreatePlayerWithPlayerInvitationCommand, CreatePlayerInfo> _createPlayerInvitationPlayerService;
    private readonly IGetService<CreatePlayerCommand, CreatePlayerInfo> _createPlayerService;
    private readonly IPlayerServiceClient _playerServiceClient;
    public string Type => "CreatePlayer";

    public CreatePlayerRegisterFlowStep(
        IGetService<CreatePlayerCommand, CreatePlayerInfo> createPlayerService,
        IGetService<CreatePlayerWithAgentInvitationCodeCommand, CreatePlayerInfo> createAgentInvitationPlayerService,
        IGetService<CreatePlayerWithPlayerInvitationCommand, CreatePlayerInfo> createPlayerInvitationPlayerService,
        IPlayerServiceClient playerServiceClient,
        IBrandServiceClient brandServiceClient)
    {
        _createPlayerService = createPlayerService ?? throw new ArgumentNullException(nameof(createPlayerService));
        _createAgentInvitationPlayerService = createAgentInvitationPlayerService ?? throw new ArgumentNullException(nameof(createAgentInvitationPlayerService));
        _createPlayerInvitationPlayerService = createPlayerInvitationPlayerService ?? throw new ArgumentNullException(nameof(createPlayerInvitationPlayerService));
        _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
        _brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
    }

    public async ValueTask<bool> ConfirmCompletedAsync(string? playerCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(playerCode))
            return false;

        var player = await _playerServiceClient
            .GetPlayerInfoAsync(playerCode, cancellationToken)
            .ConfigureAwait(false);

        return player is not null;
    }

    public async ValueTask<RegisterFlowProcessResult> ProcessInputAsync(
        Guid brandId,
        JsonDocument input,
        PlayerLocationInfo locationInfo,
        CancellationToken cancellationToken = default)
    {
        var request = input.Deserialize<CreatePlayerInputData>()!;
        var brand = await _brandServiceClient
            .GetBrandInfoAsync(brandId, cancellationToken)
            .ConfigureAwait(false);

        var agentInvitationCode = string.IsNullOrWhiteSpace(locationInfo.AgentInvitationCode)
            ? request.AgentInvitationCode
            : locationInfo.AgentInvitationCode;

        if (!string.IsNullOrWhiteSpace(request.UpperPlayerCode))
        {
            var command = new CreatePlayerWithPlayerInvitationCommand(
                brandId,
                request.UserName,
                request.Password,
                brand!.Currency,
                request.UpperPlayerCode,
                request.IsTestPlayer);

            var info = await _createPlayerInvitationPlayerService
                .GetAsync(command)
                .ConfigureAwait(false);

            if (info.IsNew)
                await _playerServiceClient
                    .SetPlayerStatusAsync(info.PlayerCode, PlayerStatus.RegisterUndone)
                    .ConfigureAwait(false);

            return new RegisterFlowProcessResult(
                info.IsNew,
                null,
                JsonSerializer.SerializeToDocument(
                    new CreatePlayerResponse
                    {
                        PlayerCode = info.PlayerCode,
                        IsNew = info.IsNew
                    }));
        }
        else if (!string.IsNullOrWhiteSpace(agentInvitationCode))
        {
            var command = new CreatePlayerWithAgentInvitationCodeCommand(
                brandId,
                request.UserName,
                request.Password,
                brand!.Currency,
                agentInvitationCode,
                request.IsTestPlayer);

            var info = await _createAgentInvitationPlayerService
                .GetAsync(command)
                .ConfigureAwait(false);

            if (info.IsNew)
                await _playerServiceClient
                    .SetPlayerStatusAsync(info.PlayerCode, PlayerStatus.RegisterUndone)
                    .ConfigureAwait(false);

            return new RegisterFlowProcessResult(
                info.IsNew,
                null,
                JsonSerializer.SerializeToDocument(
                    new CreatePlayerResponse
                    {
                        PlayerCode = info.PlayerCode,
                        IsNew = info.IsNew
                    }));
        }
        else
        {
            var query = new CreatePlayerCommand(
                brandId,
                request.UserName,
                request.Password,
                brand!.Currency,
                request.IsTestPlayer);

            var info = await _createPlayerService.GetAsync(query).ConfigureAwait(false);

            if (info.IsNew)
                await _playerServiceClient
                    .SetPlayerStatusAsync(info.PlayerCode, PlayerStatus.RegisterUndone)
                    .ConfigureAwait(false);

            return new RegisterFlowProcessResult(
                info.IsNew,
                null,
                JsonSerializer.SerializeToDocument(
                    new CreatePlayerResponse
                    {
                        PlayerCode = info.PlayerCode,
                        IsNew = info.IsNew
                    }));
        }
    }

    internal record CreatePlayerInputData
    {
        [JsonPropertyName("invitationcode")]
        public string? AgentInvitationCode { get; init; }

        [JsonPropertyName("testplayer")]
        public bool IsTestPlayer { get; init; } = false;

        [JsonPropertyName("pwd")]
        public string Password { get; init; } = default!;

        [JsonPropertyName("upper")]
        public string? UpperPlayerCode { get; init; }

        [Required]
        [JsonPropertyName("username")]
        public string UserName { get; init; } = default!;
    }

    internal record CreatePlayerResponse
    {
        [JsonPropertyName("uid")]
        public string PlayerCode { get; init; } = default!;

        [JsonPropertyName("isnew")]
        public bool IsNew { get; init; }
    }

    public void SetupStep(JsonElement jsonElement) => throw new NotImplementedException();
}
