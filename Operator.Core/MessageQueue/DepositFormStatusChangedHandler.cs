using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tgs.MessageQueue;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using TGS.Services.Operator.Notify;

namespace TGS.Services.Operator.MessageQueue;

internal class DepositFormStatusChangedHandler : IMessageHandler
{
	private readonly INotifyEventPublisher _notifyEventPublisher;
	private readonly IPaymentServiceClient _paymentServiceClient;
	private readonly JsonSerializerOptions _serializerOptions;

	public DepositFormStatusChangedHandler(
		IPaymentServiceClient paymentServiceClient,
		INotifyEventPublisher notifyEventPublisher,
		IOptions<JsonOptions> jsonOptionsAccessor)
	{
		if (jsonOptionsAccessor is null)
			throw new ArgumentNullException(nameof(jsonOptionsAccessor));
		_paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
		_notifyEventPublisher = notifyEventPublisher ?? throw new ArgumentNullException(nameof(notifyEventPublisher));

		_serializerOptions = jsonOptionsAccessor.Value.JsonSerializerOptions;
	}

	public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var formEvent = FormEventEx.Parser.ParseFrom(data.Span);
		var previousStatus = Enum.Parse<PaymentFlowState>(formEvent.Status);

		var depositForm = await _paymentServiceClient.GetDepositFormAsync(
			formEvent.FormId,
			cancellationToken).ConfigureAwait(false);

		if (depositForm is null)
			return;

		if (previousStatus is PaymentFlowState.WaitApprove
			&& depositForm.Status is PaymentFlowState.Approved or PaymentFlowState.Reject)
		{
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
		}
	}
}
