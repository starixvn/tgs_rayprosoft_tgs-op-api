using System;
using System.Threading.Tasks;
using Moq;
using NSubstitute;
using NUnit.Framework;
using Tgs.BrandCertificate.Clients;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Core.Models.Enums;
using TGS.Services.Operations;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Player;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class PlayerIdentityTests
{
    [Test]
    public void Create_Player_Identity_Should_Failed_When_Brand_Enable_Player_Identity_Is_False()
    {
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

        var fakePlayerRepository = Substitute.For<IPlayerRepository>();
        var brandId = Guid.NewGuid();

        var brandInfo = new Tgs.BrandCertificate.Clients.BrandInfo
        {
            Id = brandId,
            Code = "GPI",
            Currency = "CNY"
        };

		_ = fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId)).Returns(brandInfo);

        var sut = new CreateIdentityAuthenticationFormCommandService(fakeBrandServiceClient, fakePlayerRepository);

		_ = Assert.ThrowsAsync<BrandEnablePlayerIdentityNotSetException>(
			async () => await sut.ExecuteAsync(new CreateIdentityAuthenticationFormCommand("playerCode", brandId, "fileId")));
    }

    [Test]
    public async Task Query_Identity_Form_Should_Success()
    {
        var fakePlayerRepository = Substitute.For<IPlayerRepository>();

        _ = fakePlayerRepository
            .GetPlayerIdentityFormQueryAsync("playerCode")
            .Returns(new PlayerIdentityFormInfo(Guid.NewGuid(), PlayerIdentityReviewState.Reject));

        var sut = new GetPlayerIdentityFormQueryService(fakePlayerRepository);

        var query = new GetPlayerIdentityFormQuery("playerCode");

		_ = await sut.GetAsync(query);

        _ = fakePlayerRepository
            .Received(1)
            .GetPlayerIdentityFormQueryAsync("playerCode");
    }

    [Test]
    public async Task Set_Player_Identity_Should_Success()
    {
        var moqPlayerOperations = new Mock<IPlayerOperations>();

        var sut = new SetIdentityCommandService(moqPlayerOperations.Object);

        var brandId = Guid.NewGuid();

        var player = new Player
        {
            PlayerCode = "abcdef123456",
            Username = "Tester",
            BrandId = brandId,
            DefaultWalletCurrencyCode = "TBH"
        };
        var command = new SetIdentityCommand(player.PlayerCode, "RealName", "123456789", brandId);

		_ = moqPlayerOperations
			.Setup(fake => fake.GetPlayerByCodeAsync(
				It.Is<string>(x => x == player.PlayerCode)))
			.Returns(Task.FromResult(player));

        await sut.ExecuteAsync(command);

        moqPlayerOperations.Verify(
            fake => fake.SetIdentityAsync(
                It.Is<string>(x => x == player.PlayerCode),
                It.Is<string>(x => x == command.RealName),
                It.Is<string>(x => x == command.Identity),
                It.Is<bool>(x => x == false),
                It.IsAny<DateTimeOffset>()),
            Times.Exactly(1));
    }

    [Test]
    public async Task When_Brand_Enable_Player_Identity_Equals_True_Should_Create_Player_Identity_Success()
    {
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

        var fakePlayerRepository = Substitute.For<IPlayerRepository>();
        var brandId = Guid.NewGuid();

        var brandInfo = new Tgs.BrandCertificate.Clients.BrandInfo
        {
            Id = brandId,
            Code = "GPI",
            Currency = "CNY",
            PlayerIdentityEnabled = true
        };

        _ = fakeBrandServiceClient
            .GetBrandInfoAsync(Arg.Is(brandId))
            .Returns(brandInfo);

        var authFormId = Guid.NewGuid();
        var playerCode = "playerCode";

        _ = fakePlayerRepository
            .CreatePlayerAuthenticationFormAsync(Arg.Is(brandId), Arg.Is(playerCode), Arg.Is("{}"))
            .Returns(authFormId);

        var sut = new CreateIdentityAuthenticationFormCommandService(fakeBrandServiceClient, fakePlayerRepository);

        var command = new CreateIdentityAuthenticationFormCommand(playerCode, brandId, "620c6db1110125f95bb9ccb2");

        await sut.ExecuteAsync(command);

        _ = fakePlayerRepository
            .Received(1)
            .UpdatePlayerIdentityAsync(Arg.Is(playerCode), Arg.Any<Guid>());
    }
}
