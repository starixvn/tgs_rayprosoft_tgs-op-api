using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using TGS.Apis.OperatorApi.SignalR;
using TGS.Services.Operator.Notify;

namespace TGS.Apis.OperatorApi.Models;

public class PushNotifyfConsumer : BackgroundService
{
	private readonly IObservable<NotifyEvent> _eventObservable;
	private readonly IHubContext<NotifyHub> _hubContext;
	private readonly ILogger<PushNotifyfConsumer> _logger;

	public PushNotifyfConsumer(
		IObservable<NotifyEvent> eventObservable,
		IHubContext<NotifyHub> hubContext,
		ILogger<PushNotifyfConsumer> logger)
	{
		_eventObservable = eventObservable ?? throw new ArgumentNullException(nameof(eventObservable));
		_hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
		=> _eventObservable
			.Where(evt => evt.Match("Feature", "PushNotify"))
			.Select(evt => (
				Id: Guid.Parse(evt.GetTagValues("Id").First()),
				Type: evt.GetTagValues("Type").First(),
				Payload: evt.Body,
				Users: evt.GetTagValues("UserId")))
			.Select(tup => Observable.FromAsync(async () => await _hubContext.Clients
				.Users(tup.Users).SendAsync(
					"notify",
					new
					{
						tup.Type,
						tup.Payload
					}, stoppingToken).ConfigureAwait(false))
				.Catch((Exception ex) =>
				{
					_logger.LogError(ex, "Handle notify occur error.");

					return Observable.Empty<Unit>();
				}))
			.Merge()
			.ToTask(stoppingToken);
}
