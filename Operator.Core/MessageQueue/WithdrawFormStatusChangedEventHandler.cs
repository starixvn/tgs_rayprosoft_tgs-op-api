using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using TGS.Services.Operator.Notify;
using Valhalla.MessageQueue;

namespace TGS.Services.Operator.MessageQueue;

internal class WithdrawFormStatusChangedEventHandler : IMessageHandler<ReadOnlyMemory<byte>>
{
	private readonly INotifyEventPublisher _notifyEventPublisher;
	private readonly IPaymentServiceClient _paymentServiceClient;
	private readonly JsonSerializerOptions _serializerOptions;

	public WithdrawFormStatusChangedEventHandler(
		IPaymentServiceClient paymentServiceClient,
		INotifyEventPublisher notifyEventPublisher,
		IOptions<JsonOptions> jsonOptionsAccessor)
	{
		ArgumentNullException.ThrowIfNull(jsonOptionsAccessor);
		_paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
		_notifyEventPublisher = notifyEventPublisher ?? throw new ArgumentNullException(nameof(notifyEventPublisher));

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

		var withdrawForm = await _paymentServiceClient.GetWithdrawFormAsync(
			formEvent.FormId,
			cancellationToken).ConfigureAwait(false);

		if (withdrawForm is null || withdrawForm.UserType is not UserType.Player)
			return;

		if (previousStatus is PaymentFlowState.WaitApprove or PaymentFlowState.Approved
			&& withdrawForm.Status is PaymentFlowState.Approved or PaymentFlowState.Paid or PaymentFlowState.Reject)
		{
			var tags = new Dictionary<string, string>
			{
				["BrandId"] = withdrawForm.BrandId.ToString(),
				["UserId"] = withdrawForm.UserId ?? string.Empty,
				["Feature"] = "WithdrawNotify"
			};
			var notifyBody = new WithdrawNotifyBody(
				withdrawForm.Id,
				withdrawForm.OrderNo,
				withdrawForm.BrandPaymentId,
				withdrawForm.ActualAmount ?? withdrawForm.Amount,
				withdrawForm.Status,
				withdrawForm.UpdatedOn.UtcDateTime);

			_notifyEventPublisher.PublishEvent(new NotifyEvent(
				tags,
				JsonSerializer.SerializeToDocument(notifyBody, _serializerOptions)));
		}
	}
}
