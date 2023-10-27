using System.Collections.ObjectModel;
using Jering.Javascript.NodeJS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PhoenixToolkits.Xunit.AssemblyFixture;

namespace OperatorApi.IntegrationTests;

[AssemblyFixture]
public class NodeJsFixture : IDisposable
{
	private readonly ServiceProvider _serviceProvider;

	private bool _disposedValue;

	public NodeJsFixture()
	{
		StaticNodeJSService.Configure<NodeJSProcessOptions>(
			nodeOptions => nodeOptions.ProjectPath =
				Path.GetFullPath(Path.Combine(
					Environment.CurrentDirectory,
					"../../../..",
					"op_client")));
		StaticNodeJSService.Configure<OutOfProcessNodeJSServiceOptions>(
			processOptions => processOptions.Concurrency = Concurrency.MultiProcess);

		var services = new ServiceCollection();
		_ = services.AddNodeJS()
			.Configure<NodeJSProcessOptions>(nodeOptions => nodeOptions.ProjectPath =
			Path.GetFullPath(Path.Combine(
				Environment.CurrentDirectory,
				"../../../..",
				"bo_client")))
			.Configure<OutOfProcessNodeJSServiceOptions>(
			processOptions =>
			{
				processOptions.Concurrency = Concurrency.MultiProcess;
				//processOptions.TimeoutMS = 3000;
				processOptions.NumRetries = 0;
				processOptions.NumProcessRetries = 0;
			})
			.AddTransient<INodeJSService>(sp =>
			{
				var outOfProcessNodeJSServiceOptions = sp.GetRequiredService<IOptions<OutOfProcessNodeJSServiceOptions>>().Value;
				var environmentService = sp.GetRequiredService<IEnvironmentService>();

				var concurrencyDegree = outOfProcessNodeJSServiceOptions.ConcurrencyDegree;
				var processorCount = environmentService.ProcessorCount; // TODO to be safe we should ensure that this is >= 1

				if (outOfProcessNodeJSServiceOptions.Concurrency == Concurrency.None ||
					concurrencyDegree == 1 || // MultiProcess mode but only 1 process
					concurrencyDegree <= 0 && processorCount == 1) // Machine has only 1 logical processor
				{
					return ActivatorUtilities.CreateInstance<HttpNodeJSService>(sp);
				}
				else
				{
					if (concurrencyDegree <= 0)
						concurrencyDegree = processorCount;

					var httpNodeJSServices = new HttpNodeJSService[concurrencyDegree];
					for (var i = 0; i < concurrencyDegree; i++)
					{
						httpNodeJSServices[i] = ActivatorUtilities.CreateInstance<HttpNodeJSService>(sp);
					}

					return new HttpNodeJSPoolService(new ReadOnlyCollection<HttpNodeJSService>(httpNodeJSServices));
				}
			});

		_serviceProvider = services.BuildServiceProvider();
	}

	public INodeJSService BuildNodeJsService()
		=> _serviceProvider.GetRequiredService<INodeJSService>();

	public void Dispose()
	{
		// 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_serviceProvider.Dispose();
			}

			_disposedValue = true;
		}
	}
}
