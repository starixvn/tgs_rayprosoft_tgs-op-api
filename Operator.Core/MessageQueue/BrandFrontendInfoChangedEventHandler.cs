using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.BrandCertificateService.Protos;
using Tgs.MessageQueue;
using TGS.Services.Operator.PlayerRegisterFlow;

namespace TGS.Services.Operator.MessageQueue;

internal class BrandFrontendInfoChangedEventHandler : IMessageHandler
{
	private readonly IBrandFrontendInfoCacheCleaner _brandFrontendInfoCacheCleaner;

	public BrandFrontendInfoChangedEventHandler(IBrandFrontendInfoCacheCleaner brandFrontendInfoCacheCleaner)
	{
		_brandFrontendInfoCacheCleaner = brandFrontendInfoCacheCleaner ?? throw new ArgumentNullException(nameof(brandFrontendInfoCacheCleaner));
	}

	public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var eventData = BrandFrontendInfoChangedEvent.Parser.ParseFrom(data.Span);

		_brandFrontendInfoCacheCleaner.Clean(eventData.BrandId);

		return ValueTask.CompletedTask;
	}
}
