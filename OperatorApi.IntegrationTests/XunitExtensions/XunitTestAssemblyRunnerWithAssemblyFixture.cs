using Jering.Javascript.NodeJS;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace OperatorApi.IntegrationTests.XunitExtensions;

public class XunitTestAssemblyRunnerWithAssemblyFixture : XunitTestAssemblyRunner
{
	private readonly Dictionary<Type, object> _assemblyFixtureMappings = new();

	public XunitTestAssemblyRunnerWithAssemblyFixture(
		ITestAssembly testAssembly,
		IEnumerable<IXunitTestCase> testCases,
		IMessageSink diagnosticMessageSink,
		IMessageSink executionMessageSink,
		ITestFrameworkExecutionOptions executionOptions)
		: base(
			  testAssembly,
			  testCases,
			  diagnosticMessageSink,
			  executionMessageSink,
			  executionOptions)
	{ }

	protected override async Task AfterTestAssemblyStartingAsync()
	{
		// Let everything initialize
		await base.AfterTestAssemblyStartingAsync();

		// Go find all the AssemblyFixtureAttributes adorned on the test assembly
		Aggregator.Run(() =>
		{
			StaticNodeJSService.Configure<NodeJSProcessOptions>(
				nodeOptions => nodeOptions.ProjectPath =
					Path.GetFullPath(Path.Combine(
						Environment.CurrentDirectory,
						"../../../..",
						"op_client")));
			StaticNodeJSService.Configure<OutOfProcessNodeJSServiceOptions>(
				 processOptions => processOptions.Concurrency = Concurrency.MultiProcess);
		});
	}

	protected override Task BeforeTestAssemblyFinishedAsync()
	{
		// Make sure we clean up everybody who is disposable, and use Aggregator.Run to isolate Dispose failures
		foreach (var disposable in _assemblyFixtureMappings.Values.OfType<IDisposable>())
			Aggregator.Run(disposable.Dispose);

		return base.BeforeTestAssemblyFinishedAsync();
	}

	protected override Task<RunSummary> RunTestCollectionAsync(
		IMessageBus messageBus,
		ITestCollection testCollection,
		IEnumerable<IXunitTestCase> testCases,
		CancellationTokenSource cancellationTokenSource)
		=> new XunitTestCollectionRunnerWithAssemblyFixture(
			_assemblyFixtureMappings,
			testCollection,
			testCases,
			DiagnosticMessageSink,
			messageBus,
			TestCaseOrderer,
			new ExceptionAggregator(Aggregator),
			cancellationTokenSource).RunAsync();
}
