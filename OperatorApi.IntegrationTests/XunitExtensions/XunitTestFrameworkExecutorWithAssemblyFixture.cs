using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;
using OperatorApi.IntegrationTests.XunitExtensions;

namespace OperatorApi.IntegrationTests.XunitExtensions;

public class XunitTestFrameworkExecutorWithAssemblyFixture : XunitTestFrameworkExecutor
{
	public XunitTestFrameworkExecutorWithAssemblyFixture(
		AssemblyName assemblyName,
		ISourceInformationProvider sourceInformationProvider,
		IMessageSink diagnosticMessageSink)
		: base(
			  assemblyName,
			  sourceInformationProvider,
			  diagnosticMessageSink)
	{ }

	protected override async void RunTestCases(IEnumerable<IXunitTestCase> testCases, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
	{
		using var assemblyRunner = new XunitTestAssemblyRunnerWithAssemblyFixture(
			TestAssembly,
			testCases,
			DiagnosticMessageSink,
			executionMessageSink,
			executionOptions);

		_ = await assemblyRunner.RunAsync();
	}
}
