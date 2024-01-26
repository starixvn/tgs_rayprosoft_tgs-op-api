using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using NUnit.Framework.Legacy;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;
using TGS.Apis.OperatorApi.Attributes;
using TGS.Apis.OperatorApi.Filters;
using TGS.Apis.OperatorApi.Models;
using TGS.Core.Models.Tokens;
using TGS.Services.Operator.ApplicationServices.Player;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class PlayerStatusCheckTests
{
	[Test]
	public async Task When_Api_Has_AuthorizePlayerTokenAttribute_And_AllowNullOrEmpty_Is_False_And_NeedAuthorize_Is_False_Should_Execute()
	{
		var fakeService = Substitute.For<IGetService<GetPlayerStatusQuery, PlayerStatus>>();
		var sut = new CheckPlayerEnabledFilter(fakeService);
		var fakeHttpContext = new DefaultHttpContext();

		fakeHttpContext.Items.Add("playerToken", new PlayerTokenData()
		{
			PlayerCode = "playerCode"
		});
		var routeData = new RouteData();
		var actionDescriptor = new ActionDescriptor();
		var actionContext = new ActionContext(fakeHttpContext, routeData, actionDescriptor);
		var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata> { });
		context.ActionDescriptor.EndpointMetadata = new List<object> {
			new AuthorizePlayerTokenAttribute()
			{
				NeedAuthorize = false,
				AllowNullOrEmpty = false
			},
		};
		await sut.OnAuthorizationAsync(context);

		ClassicAssert.IsNull(context.Result);
	}

	[Test]
	public async Task When_Api_Has_AuthorizePlayerTokenAttribute_And_AllowNullOrEmpty_Is_False_And_NeedAuthorize_Is_True_Should_Execute()
	{
		var fakeService = Substitute.For<IGetService<GetPlayerStatusQuery, PlayerStatus>>();
		_ = fakeService
			.GetAsync(new GetPlayerStatusQuery("playerCode"))
			.Returns(PlayerStatus.Disabled);
		var sut = new CheckPlayerEnabledFilter(fakeService);
		var fakeHttpContext = new DefaultHttpContext();

		fakeHttpContext.Items.Add("playerToken", new PlayerTokenData()
		{
			PlayerCode = "playerCode"
		});
		var routeData = new RouteData();
		var actionDescriptor = new ActionDescriptor();
		var actionContext = new ActionContext(fakeHttpContext, routeData, actionDescriptor);
		var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata> { });
		context.ActionDescriptor.EndpointMetadata = new List<object> {
			new AuthorizePlayerTokenAttribute()
			{
				AllowNullOrEmpty = false,
				NeedAuthorize = true
			},
		};
		await sut.OnAuthorizationAsync(context);

		ClassicAssert.IsNull(context.Result);
	}

	[Test]
	public async Task When_Api_Has_AuthorizePlayerTokenAttribute_And_AllowNullOrEmpty_Is_True_And_NeedAuthorize_Is_False__Should_Execute()
	{
		var fakeService = Substitute.For<IGetService<GetPlayerStatusQuery, PlayerStatus>>();
		var sut = new CheckPlayerEnabledFilter(fakeService);
		var fakeHttpContext = new DefaultHttpContext();

		fakeHttpContext.Items.Add("playerToken", new PlayerTokenData()
		{
			PlayerCode = "playerCode"
		});
		var routeData = new RouteData();
		var actionDescriptor = new ActionDescriptor();
		var actionContext = new ActionContext(fakeHttpContext, routeData, actionDescriptor);
		var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata> { });
		context.ActionDescriptor.EndpointMetadata = new List<object> {
			new AuthorizePlayerTokenAttribute()
			{
				AllowNullOrEmpty = true,
				NeedAuthorize = false
			},
		};
		await sut.OnAuthorizationAsync(context);

		ClassicAssert.IsNull(context.Result);
	}

	[Test]
	public async Task When_Api_Has_AuthorizePlayerTokenAttribute_And_AllowNullOrEmpty_Is_True_And_NeedAuthorize_Is_True_Should_Execute()
	{
		var fakeService = Substitute.For<IGetService<GetPlayerStatusQuery, PlayerStatus>>();
		var sut = new CheckPlayerEnabledFilter(fakeService);
		var fakeHttpContext = new DefaultHttpContext();

		fakeHttpContext.Items.Add("playerToken", new PlayerTokenData()
		{
			PlayerCode = "playerCode"
		});
		var routeData = new RouteData();
		var actionDescriptor = new ActionDescriptor();
		var actionContext = new ActionContext(fakeHttpContext, routeData, actionDescriptor);
		var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata> { });
		context.ActionDescriptor.EndpointMetadata = new List<object> {
			new AuthorizePlayerTokenAttribute()
			{
				AllowNullOrEmpty = true,
				NeedAuthorize = true
			},
		};
		await sut.OnAuthorizationAsync(context);

		ClassicAssert.IsNull(context.Result);
	}

	[Test]
	public async Task When_Api_Has_AuthorizePlayerTokenAttribute_And_NeedAuthorize_Is_True_And_AllowNullOrEmpty_Is_False_And_Player_Status_Blacklist_Should_Get_StatusCodes_403()
	{
		var fakeService = Substitute.For<IGetService<GetPlayerStatusQuery, PlayerStatus>>();
		_ = fakeService
			.GetAsync(new GetPlayerStatusQuery("playerCode"))
			.Returns(PlayerStatus.InBlacklist);
		var sut = new CheckPlayerEnabledFilter(fakeService);
		var fakeHttpContext = new DefaultHttpContext();

		fakeHttpContext.Items.Add("playerToken", new PlayerTokenData()
		{
			PlayerCode = "playerCode"
		});
		var routeData = new RouteData();
		var actionDescriptor = new ActionDescriptor();
		var actionContext = new ActionContext(fakeHttpContext, routeData, actionDescriptor);
		var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata> { });
		context.ActionDescriptor.EndpointMetadata = new List<object> {
			new AuthorizePlayerTokenAttribute()
			{
				AllowNullOrEmpty = false,
				NeedAuthorize = true
			},
		};
		await sut.OnAuthorizationAsync(context);

		ClassicAssert.IsNotNull(context.Result);

		var result = (ObjectResult)context.Result;

		ClassicAssert.AreEqual(StatusCodes.Status403Forbidden, result.StatusCode);
		ClassicAssert.IsInstanceOf<FailResponse>(result.Value);
		ClassicAssert.AreEqual("AccessDenied", ((FailResponse)result.Value).ErrorMessageKey);
	}

	[Test]
	public async Task When_Api_Has_No_AuthorizePlayerTokenAttribute_Should_Execute()
	{
		var fakeService = Substitute.For<IGetService<GetPlayerStatusQuery, PlayerStatus>>();
		var sut = new CheckPlayerEnabledFilter(fakeService);
		var fakeHttpContext = new DefaultHttpContext();

		fakeHttpContext.Items.Add("playerToken", new PlayerTokenData()
		{
			PlayerCode = "playerCode"
		});
		var routeData = new RouteData();
		var actionDescriptor = new ActionDescriptor();
		var actionContext = new ActionContext(fakeHttpContext, routeData, actionDescriptor);
		var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata> { });
		await sut.OnAuthorizationAsync(context);

		ClassicAssert.IsNull(context.Result);
	}

	[Test]
	public async Task When_Player_Status_Blacklist_And_Api_Has_IgnoreUserBlacklistCheckAttribute_Should_Execute()
	{
		var fakeService = Substitute.For<IGetService<GetPlayerStatusQuery, PlayerStatus>>();

		_ = fakeService
			.GetAsync(new GetPlayerStatusQuery("playerCode"))
			.Returns(PlayerStatus.InBlacklist);

		var sut = new CheckPlayerEnabledFilter(fakeService);

		var fakeHttpContext = new DefaultHttpContext();

		fakeHttpContext.Items.Add("playerToken", new PlayerTokenData()
		{
			PlayerCode = "playerCode"
		});

		var routeData = new RouteData();
		var actionDescriptor = new ActionDescriptor();
		var actionContext = new ActionContext(fakeHttpContext, routeData, actionDescriptor);
		var context = new AuthorizationFilterContext(actionContext, Array.Empty<IFilterMetadata>())
		{
			ActionDescriptor = {
				EndpointMetadata = new List<object>
				{
					new AuthorizePlayerTokenAttribute
					{
						AllowNullOrEmpty = false,
						NeedAuthorize = true
					},
					new IgnoreUserBlacklistCheckAttribute()
				}
			}
		};

		await sut.OnAuthorizationAsync(context);

		ClassicAssert.IsNull(context.Result);
	}

	[Test]
	public async Task When_Player_Status_BlackList_Should_Get_StatusCodes_403()
	{
		var fakeService = Substitute.For<IGetService<GetPlayerStatusQuery, PlayerStatus>>();

		_ = fakeService
			.GetAsync(new GetPlayerStatusQuery("playerCode"))
			.Returns(PlayerStatus.InBlacklist);
		var sut = new CheckPlayerEnabledFilter(fakeService);

		var fakeHttpContext = new DefaultHttpContext();

		fakeHttpContext.Items.Add("playerToken", new PlayerTokenData()
		{
			PlayerCode = "playerCode"
		});
		var routeData = new RouteData();
		var actionDescriptor = new ActionDescriptor();
		var actionContext = new ActionContext(fakeHttpContext, routeData, actionDescriptor);
		var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata> { });
		context.ActionDescriptor.EndpointMetadata = new List<object> {
			new AuthorizePlayerTokenAttribute()
			{
				AllowNullOrEmpty = false,
				NeedAuthorize = true
			}
		};

		await sut.OnAuthorizationAsync(context);

		ClassicAssert.IsNotNull(context.Result);

		var result = (ObjectResult)context.Result;

		ClassicAssert.AreEqual(StatusCodes.Status403Forbidden, result.StatusCode);
		ClassicAssert.IsInstanceOf<FailResponse>(result.Value);
		ClassicAssert.AreEqual("AccessDenied", ((FailResponse)result.Value).ErrorMessageKey);
	}

	[Test]
	public async Task When_Player_Status_Disabled_And_Api_Has_EnabledUserBlockedAttribute_Should_Get_StatusCodes_403()
	{
		var fakeService = Substitute.For<IGetService<GetPlayerStatusQuery, PlayerStatus>>();
		_ = fakeService
			.GetAsync(new GetPlayerStatusQuery("playerCode"))
			.Returns(PlayerStatus.Disabled);

		var sut = new CheckPlayerEnabledFilter(fakeService);

		var fakeHttpContext = new DefaultHttpContext();

		fakeHttpContext.Items.Add("playerToken", new PlayerTokenData()
		{
			PlayerCode = "playerCode"
		});
		var routeData = new RouteData();
		var actionDescriptor = new ActionDescriptor();
		var actionContext = new ActionContext(fakeHttpContext, routeData, actionDescriptor);
		var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata> { });
		context.ActionDescriptor.EndpointMetadata = new List<object> {
			new AuthorizePlayerTokenAttribute()
			{
				AllowNullOrEmpty = false,
				NeedAuthorize = true
			},
			new EnableUserBlockedCheckAttribute()
		};
		await sut.OnAuthorizationAsync(context);

		ClassicAssert.IsNotNull(context.Result);

		var result = (ObjectResult)context.Result;

		ClassicAssert.AreEqual(StatusCodes.Status403Forbidden, result.StatusCode);
		ClassicAssert.IsInstanceOf<FailResponse>(result.Value);
		ClassicAssert.AreEqual("UserBlocked", ((FailResponse)result.Value).ErrorMessageKey);
	}

	[Test]
	public async Task When_Player_Status_Disabled_And_Api_Has_No_CheckEnabledAttribute_Should_Execute()
	{
		var fakeService = Substitute.For<IGetService<GetPlayerStatusQuery, PlayerStatus>>();
		_ = fakeService
			.GetAsync(new GetPlayerStatusQuery("playerCode"))
			.Returns(PlayerStatus.Disabled);

		var sut = new CheckPlayerEnabledFilter(fakeService);

		var fakeHttpContext = new DefaultHttpContext();

		fakeHttpContext.Items.Add("playerToken", new PlayerTokenData()
		{
			PlayerCode = "playerCode"
		});
		var routeData = new RouteData();
		var actionDescriptor = new ActionDescriptor();
		var actionContext = new ActionContext(fakeHttpContext, routeData, actionDescriptor);
		var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata> { });
		context.ActionDescriptor.EndpointMetadata = new List<object> {
			new AuthorizePlayerTokenAttribute()
			{
				AllowNullOrEmpty = false,
				NeedAuthorize = true
			}
		};
		await sut.OnAuthorizationAsync(context);

		ClassicAssert.IsNull(context.Result);
	}

	[Test]
	public async Task When_Player_Status_Enabled_Api_Should_Execute()
	{
		var fakeService = Substitute.For<IGetService<GetPlayerStatusQuery, PlayerStatus>>();
		_ = fakeService
			.GetAsync(new GetPlayerStatusQuery("playerCode"))
			.Returns(PlayerStatus.Enabled);

		var sut = new CheckPlayerEnabledFilter(fakeService);

		var fakeHttpContext = new DefaultHttpContext();

		fakeHttpContext.Items.Add("playerToken", new PlayerTokenData()
		{
			PlayerCode = "playerCode"
		});
		var routeData = new RouteData();
		var actionDescriptor = new ActionDescriptor();
		var actionContext = new ActionContext(fakeHttpContext, routeData, actionDescriptor);
		var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata> { });
		context.ActionDescriptor.EndpointMetadata = new List<object> {
			new AuthorizePlayerTokenAttribute()
			{
				AllowNullOrEmpty = false,
				NeedAuthorize = true
			},
		};

		await sut.OnAuthorizationAsync(context);
		ClassicAssert.IsNull(context.Result);
	}
}
