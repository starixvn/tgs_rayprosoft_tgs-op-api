using System.Collections.Immutable;
using Tgs.BrandCertificate.Clients;
using Tgs.PlayerService.Client;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Player;
using Tgs.ServiceUtility;
using BrandInfo = Tgs.BrandCertificate.Clients.BrandInfo;

namespace TGS.Tests.OperatorApi.UnitTests.ApplicationServices.Player;

[TestFixture]
[TestOf(typeof(RegisterByPhoneCommandService))]
public class RegisterByPhoneCommandServiceTest
{
	private IBrandServiceClient _brandServiceClient;
	private IGetService<CreatePlayerWithPlayerInvitationCommand, CreatePlayerInfo> _createPlayerInvitationPlayerService;
	private IPlayerServiceClient _playerServiceClient;
	private IGetService<CreatePlayerWithAgentInvitationCodeCommand, CreatePlayerInfo> _createAgentInvitationPlayerService;
	private IGetService<CreatePlayerCommand, CreatePlayerInfo> _createPlayerService;

	[SetUp]
	public void SetUp()
	{
		_brandServiceClient = Substitute.For<IBrandServiceClient>();
		_createPlayerInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithPlayerInvitationCommand, CreatePlayerInfo>>();
		_playerServiceClient = Substitute.For<IPlayerServiceClient>();
		_createAgentInvitationPlayerService = Substitute.For<IGetService<CreatePlayerWithAgentInvitationCodeCommand, CreatePlayerInfo>>();
		_createPlayerService = Substitute.For<IGetService<CreatePlayerCommand, CreatePlayerInfo>>();
	}

	[Test]
	public async Task SuccessWhenUpperPlayerCodeIsNotNull()
	{
		// Arrange
		var query = new RegisterByPhoneCommand("UserName",
			"Password",
			"AreaCode",
			"Phone",
			"",
			"UpperPlayerCode",
			Guid.NewGuid(),
			"IpAddress",
			"HostName");

		var brand = new BrandInfo();
		var createPlayerInfo = new CreatePlayerInfo(PlayerCode: "PlayerCode", IsNew: true);
		_brandServiceClient.GetBrandInfoAsync(query.BrandId, Arg.Any<CancellationToken>()).Returns(brand);
		_createPlayerInvitationPlayerService.GetAsync(Arg.Any<CreatePlayerWithPlayerInvitationCommand>(), Arg.Any<CancellationToken>()).Returns(createPlayerInfo);
		_createPlayerService.GetAsync(Arg.Any<CreatePlayerCommand>(), Arg.Any<CancellationToken>()).Returns(createPlayerInfo);
		var service = new RegisterByPhoneCommandService(_brandServiceClient, _createPlayerInvitationPlayerService, _playerServiceClient, _createAgentInvitationPlayerService, _createPlayerService);

		// Act
		await service.ExecuteAsync(query);

		// Assert
		_createPlayerInvitationPlayerService.Received(1).GetAsync(Arg.Any<CreatePlayerWithPlayerInvitationCommand>(), Arg.Any<CancellationToken>());
		_createAgentInvitationPlayerService.DidNotReceive().GetAsync(Arg.Any<CreatePlayerWithAgentInvitationCodeCommand>(), Arg.Any<CancellationToken>());
		_createPlayerService.DidNotReceive().GetAsync(Arg.Any<CreatePlayerCommand>(), Arg.Any<CancellationToken>());
		AssertSuccessCase(query);
	}

	[Test]
	public async Task SuccessWhenAgentInvitationCodeIsNotNull()
	{
		// Arrange
		var query = new RegisterByPhoneCommand("UserName",
			"Password",
			"AreaCode",
			"Phone",
			"AgentInvitationCode",
			"",
			Guid.NewGuid(),
			"IpAddress",
			"HostName");

		var brand = new BrandInfo();
		var createPlayerInfo = new CreatePlayerInfo(PlayerCode: "PlayerCode", IsNew: true);
		_brandServiceClient.GetBrandInfoAsync(query.BrandId, Arg.Any<CancellationToken>()).Returns(brand);
		_createAgentInvitationPlayerService.GetAsync(Arg.Any<CreatePlayerWithAgentInvitationCodeCommand>(), Arg.Any<CancellationToken>()).Returns(createPlayerInfo);
		_createPlayerService.GetAsync(Arg.Any<CreatePlayerCommand>(), Arg.Any<CancellationToken>()).Returns(createPlayerInfo);
		var service = new RegisterByPhoneCommandService(_brandServiceClient, _createPlayerInvitationPlayerService, _playerServiceClient, _createAgentInvitationPlayerService, _createPlayerService);

		// Act
		await service.ExecuteAsync(query);

		// Assert
		_createAgentInvitationPlayerService.Received(1).GetAsync(Arg.Any<CreatePlayerWithAgentInvitationCodeCommand>(), Arg.Any<CancellationToken>());
		_createPlayerInvitationPlayerService.DidNotReceive().GetAsync(Arg.Any<CreatePlayerWithPlayerInvitationCommand>(), Arg.Any<CancellationToken>());
		_createPlayerService.DidNotReceive().GetAsync(Arg.Any<CreatePlayerCommand>(), Arg.Any<CancellationToken>());
		AssertSuccessCase(query);
	}

	private void AssertSuccessCase(RegisterByPhoneCommand query)
	{
		_playerServiceClient.Received(1).SetPlayerPropertiesAsync("PlayerCode",
			Arg.Is<IImmutableDictionary<PlayerPropertyName, string>>(dict => dict[PlayerPropertyName.AreaCode] == query.AreaCode &&
				dict[PlayerPropertyName.Phone] == query.Phone &&
				dict[PlayerPropertyName.PhoneAuthentication] == true.ToString()),
			null,
			Arg.Any<CancellationToken>());

		_playerServiceClient.Received(1).SetPlayerStatusAsync("PlayerCode", PlayerStatus.Enabled, null, Arg.Any<CancellationToken>());
		_playerServiceClient.Received(1).RecordRegisterIpAsync("PlayerCode", "IpAddress", "HostName", Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task SuccessWhenUpperPlayerCodeAndAgentInvitationCodeAreNull()
	{
		// Arrange
		var query = new RegisterByPhoneCommand("UserName",
			"Password",
			"AreaCode",
			"Phone",
			"",
			"",
			Guid.NewGuid(),
			"IpAddress",
			"HostName");

		var brand = new BrandInfo();
		var createPlayerInfo = new CreatePlayerInfo(PlayerCode: "PlayerCode", IsNew: true);
		_brandServiceClient.GetBrandInfoAsync(query.BrandId, Arg.Any<CancellationToken>()).Returns(brand);
		_createPlayerService.GetAsync(Arg.Any<CreatePlayerCommand>(), Arg.Any<CancellationToken>()).Returns(createPlayerInfo);
		var service = new RegisterByPhoneCommandService(_brandServiceClient, _createPlayerInvitationPlayerService, _playerServiceClient, _createAgentInvitationPlayerService, _createPlayerService);

		// Act
		await service.ExecuteAsync(query);

		// Assert
		_createPlayerService.Received(1).GetAsync(Arg.Any<CreatePlayerCommand>(), Arg.Any<CancellationToken>());
		_createPlayerInvitationPlayerService.DidNotReceive().GetAsync(Arg.Any<CreatePlayerWithPlayerInvitationCommand>(), Arg.Any<CancellationToken>());
		_createAgentInvitationPlayerService.DidNotReceive().GetAsync(Arg.Any<CreatePlayerWithAgentInvitationCodeCommand>(), Arg.Any<CancellationToken>());
		AssertSuccessCase(query);
	}

	[Test]
	public void ThrowPlayerUserNameDuplicateExceptionWhenPlayerIsNotNew()
	{
		// Arrange
		var query = new RegisterByPhoneCommand("UserName",
			"Password",
			"AreaCode",
			"Phone",
			"",
			"",
			Guid.NewGuid(),
			"IpAddress",
			"HostName");

		var brand = new BrandInfo();
		var createPlayerInfo = new CreatePlayerInfo(PlayerCode: "PlayerCode", IsNew: false);
		_brandServiceClient.GetBrandInfoAsync(query.BrandId, Arg.Any<CancellationToken>()).Returns(brand);
		_createPlayerService.GetAsync(Arg.Any<CreatePlayerCommand>(), Arg.Any<CancellationToken>()).Returns(createPlayerInfo);
		var sut = new RegisterByPhoneCommandService(_brandServiceClient, _createPlayerInvitationPlayerService, _playerServiceClient, _createAgentInvitationPlayerService, _createPlayerService);

		// Act
		Assert.ThrowsAsync<PlayerUserNameDuplicateException>(() => sut.ExecuteAsync(query).AsTask());

		// Assert
		_createPlayerService.Received(1).GetAsync(Arg.Any<CreatePlayerCommand>(), Arg.Any<CancellationToken>());
		_createPlayerInvitationPlayerService.DidNotReceive().GetAsync(Arg.Any<CreatePlayerWithPlayerInvitationCommand>(), Arg.Any<CancellationToken>());
		_createAgentInvitationPlayerService.DidNotReceive().GetAsync(Arg.Any<CreatePlayerWithAgentInvitationCodeCommand>(), Arg.Any<CancellationToken>());
	}
}