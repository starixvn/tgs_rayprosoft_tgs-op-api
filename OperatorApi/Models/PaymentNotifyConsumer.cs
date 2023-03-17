using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using TGS.Apis.OperatorApi.SignalR;
using TGS.Services.Operator;
using TGS.Services.Operator.Notify;

namespace TGS.Apis.OperatorApi.Models;

public class PaymentNotifyConsumer : BackgroundService
{
	private readonly IObservable<NotifyEvent> _eventObservable;
	private readonly IHubContext<NotifyHub> _hubContext;
	private readonly JsonSerializerOptions _serializerOptions;

	public PaymentNotifyConsumer(
		IObservable<NotifyEvent> eventObservable,
		IHubContext<NotifyHub> hubContext,
		IOptions<JsonOptions> jsonOptionsAccessor)
	{
		if (jsonOptionsAccessor is null)
			throw new ArgumentNullException(nameof(jsonOptionsAccessor));
		_eventObservable = eventObservable ?? throw new ArgumentNullException(nameof(eventObservable));
		_hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));

		_serializerOptions = jsonOptionsAccessor.Value.JsonSerializerOptions;
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
		=> Observable.Merge(
			_eventObservable
				.Where(evt => evt.Match("Feature", "DepositNotify"))
				.Select(evt => (
					Payload: evt.Body.Deserialize<DepositNotifyBody>(_serializerOptions)!,
					Users: evt.GetTagValues("UserId")))
				.Select(tup => Observable.FromAsync(async () => await _hubContext.Clients
					.Users(tup.Users).SendAsync(
						"notify",
						new
						{
							Type = "DepositNotify",
							tup.Payload
						}, stoppingToken).ConfigureAwait(false)))
				.Merge(),
			_eventObservable
				.Where(evt => evt.Match("Feature", "WithdrawNotify"))
				.Select(evt => (
					Payload: evt.Body.Deserialize<WithdrawNotifyBody>(_serializerOptions)!,
					Users: evt.GetTagValues("UserId")))
				.Select(tup => Observable.FromAsync(async () => await _hubContext.Clients
					.Users(tup.Users).SendAsync(
						"notify",
						new
						{
							Type = "WithdrawNotify",
							tup.Payload
						}, stoppingToken).ConfigureAwait(false)))
				.Merge()).ToTask(stoppingToken);
}
