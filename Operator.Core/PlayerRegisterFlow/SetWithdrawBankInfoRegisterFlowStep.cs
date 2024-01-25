using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;
using TGS.Services.Operator.ApplicationServices.Withdraw;

namespace TGS.Services.Operator.PlayerRegisterFlow;

internal class SetWithdrawBankInfoRegisterFlowStep(
	IQueryService<GetPlayerBankInfosQuery, PlayerBankInfo> queryPlayerBankInfoService,
	ICommandService<CreateInitBankInfoCommand> createBankInfoService) : IRegisterFlowStep
{
	private readonly ICommandService<CreateInitBankInfoCommand> _createBankInfoService = createBankInfoService ?? throw new ArgumentNullException(nameof(createBankInfoService));
	private readonly IQueryService<GetPlayerBankInfosQuery, PlayerBankInfo> _queryPlayerBankInfoService = queryPlayerBankInfoService ?? throw new ArgumentNullException(nameof(queryPlayerBankInfoService));
	public string Type => "SetWithdrawBankInfo";

	public ValueTask<bool> ConfirmCompletedAsync(string? playerCode, CancellationToken cancellationToken = default)
		=> playerCode is null
			? ValueTask.FromResult(false)
			: _queryPlayerBankInfoService
				.QueryAsync(new GetPlayerBankInfosQuery(playerCode), cancellationToken)
				.AnyAsync(info => info.IsDefault, cancellationToken: cancellationToken);

	public async ValueTask<RegisterFlowProcessResult> ProcessInputAsync(
		Guid brandId,
		JsonDocument input,
		PlayerLocationInfo locationInfo,
		CancellationToken cancellationToken = default)
	{
		var request = input.Deserialize<BankInfoData>()!;

		if (string.IsNullOrWhiteSpace(locationInfo.PlayerCode))
			return new RegisterFlowProcessResult(false, null, null);

		var command = new CreateInitBankInfoCommand(
			brandId,
			locationInfo.PlayerCode,
			request.PaymentChannelId,
			request.BankType,
			request.Detail);

		await _createBankInfoService
			.ExecuteAsync(command, cancellationToken)
			.ConfigureAwait(false);

		return new RegisterFlowProcessResult(true, null, null);
	}

	internal record BankInfoData
	{
		[JsonPropertyName("banktype")]
		public string BankType { get; init; } = default!;

		[JsonPropertyName("detail")]
		public JsonDocument Detail { get; init; } = default!;

		[JsonPropertyName("paymentchannelid")]
		public Guid PaymentChannelId { get; init; }
	}
}
