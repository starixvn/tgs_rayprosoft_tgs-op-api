using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using TGS.Services.Operator.Notify;
using Valhalla.MessageQueue;

namespace TGS.Services.Operator.MessageQueue;

internal class DepositFormStatusChangedHandler : IMessageHandler<ReadOnlyMemory<byte>>
{
	private readonly ILogger<DepositFormStatusChangedHandler> _logger;
	private readonly INotifyEventPublisher _notifyEventPublisher;
	private readonly IPaymentServiceClient _paymentServiceClient;
	private readonly JsonSerializerOptions _serializerOptions;

	public DepositFormStatusChangedHandler(
		IPaymentServiceClient paymentServiceClient,
		INotifyEventPublisher notifyEventPublisher,
		IOptions<JsonOptions> jsonOptionsAccessor,
		ILogger<DepositFormStatusChangedHandler> logger)
	{
		ArgumentNullException.ThrowIfNull(jsonOptionsAccessor);
		_paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
		_notifyEventPublisher = notifyEventPublisher ?? throw new ArgumentNullException(nameof(notifyEventPublisher));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_serializerOptions = jsonOptionsAccessor.Value.JsonSerializerOptions;
	}

	public async ValueTask HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var formEvent = FormEventEx.Parser.ParseFrom(data.Span);
		var previousStatus = Enum.Parse<PaymentFlowState>(formEvent.Status);

		var depositForm = await _paymentServiceClient.GetDepositFormAsync(
			formEvent.FormId,
			cancellationToken).ConfigureAwait(false);

		if (depositForm is null)
			return;

		_logger.LogDebug("Deposit form changed: {formId} {previous} -> {current}", formEvent.FormId.ToGuid(), previousStatus, depositForm.Status);

		if (previousStatus is PaymentFlowState.WaitApprove
			&& depositForm.Status is PaymentFlowState.Approved or PaymentFlowState.Reject)
		{
			_logger.LogDebug("Send deposit notify");

			var tags = new Dictionary<string, string>
			{
				["BrandId"] = depositForm.BrandId.ToString(),
				["UserId"] = depositForm.PlayerCode,
				["Feature"] = "DepositNotify"
			};
			var notifyBody = new DepositNotifyBody(
				depositForm.Id,
				depositForm.OrderNo,
				depositForm.BrandPaymentId,
				depositForm.ActualAmount ?? depositForm.Amount,
				depositForm.Status,
				depositForm.UpdatedOn.UtcDateTime);

			_notifyEventPublisher.PublishEvent(new NotifyEvent(
				tags,
				JsonSerializer.SerializeToDocument(notifyBody, _serializerOptions)));

			_logger.LogDebug("Send deposit notify end");
		}
	}
}