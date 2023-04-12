#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Filters;
using TGS.Core.Models.Tokens;
using TGS.Services.Operator.ApplicationServices.Player;
using RouteData = Microsoft.AspNetCore.Routing.RouteData;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class PublishLoggedInEventTest
{
	[Test]
	public async Task 若HttpContext有PlayerToken時會發送登入Event()
	{
		var fakePublishEventCommandService = Substitute.For<ICommandService<PublishPlayerLoggedInEventCommand>>();

		var sut = new PublishPlayerLoggedInEventFilter(NullLogger<PublishPlayerLoggedInEventFilter>.Instance);

		var playerCode = "playerCode";

		var serviceCollection = new ServiceCollection();
		_ = serviceCollection.AddSingleton(fakePublishEventCommandService);
		var serviceProvider = serviceCollection.BuildServiceProvider();

		var fakeHttpContext = new DefaultHttpContext
		{
			RequestServices = serviceProvider
		};

		fakeHttpContext.Items.Add(
			"playerToken",
			new PlayerTokenData
			{
				PlayerCode = playerCode
			});

		var actionContext = new ActionContext(
			fakeHttpContext,
			new RouteData(),
			new ActionDescriptor(),
			new ModelStateDictionary());

		var filters = Array.Empty<IFilterMetadata>();
		var controller = new object();

		var actionExecutingContext = new ActionExecutingContext(
			actionContext,
			filters,
			new Dictionary<string, object?>(),
			controller);

		Task<ActionExecutedContext> actionExecutionDelegate() =>
			Task.FromResult(new ActionExecutedContext(actionContext, filters, controller));

		await sut.OnActionExecutionAsync(actionExecutingContext, actionExecutionDelegate);

		_ = fakePublishEventCommandService.Received(1)
			.ExecuteAsync(
				Arg.Is<PublishPlayerLoggedInEventCommand>(x => x.PlayerCode == playerCode),
				Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task 若HttpContext沒有PlayerToken時不發送登入Event()
	{
		var fakePublishEventCommandService = Substitute.For<ICommandService<PublishPlayerLoggedInEventCommand>>();

		var sut = new PublishPlayerLoggedInEventFilter(NullLogger<PublishPlayerLoggedInEventFilter>.Instance);

		var serviceCollection = new ServiceCollection();
		_ = serviceCollection.AddSingleton(fakePublishEventCommandService);
		var serviceProvider = serviceCollection.BuildServiceProvider();

		var fakeHttpContext = new DefaultHttpContext
		{
			RequestServices = serviceProvider
		};

		var actionContext = new ActionContext(
			fakeHttpContext,
			new RouteData(),
			new ActionDescriptor(),
			new ModelStateDictionary());

		var filters = Array.Empty<IFilterMetadata>();
		var controller = new object();

		var actionExecutingContext = new ActionExecutingContext(
			actionContext,
			filters,
			new Dictionary<string, object?>(),
			controller);

		Task<ActionExecutedContext> actionExecutionDelegate() =>
			Task.FromResult(new ActionExecutedContext(actionContext, filters, controller));

		await sut.OnActionExecutionAsync(actionExecutingContext, actionExecutionDelegate);

		_ = fakePublishEventCommandService.DidNotReceive()
			.ExecuteAsync(
				Arg.Any<PublishPlayerLoggedInEventCommand>(),
				Arg.Any<CancellationToken>());
	}
}
