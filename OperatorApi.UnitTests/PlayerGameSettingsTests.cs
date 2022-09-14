using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Player;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class PlayerGameSettingsTests
{
    [Test]
    public async Task Can_Get_Player_IsAutoTransferToGame_Value()
    {
        var fakePlayerRepository = Substitute.For<IPlayerRepository>();

        var sut = new GetPlayerIsAutoTransferToGameQueryService(fakePlayerRepository);

        var query = new GetPlayerIsAutoTransferToGameQuery("ThisIsPlayerCode");

        fakePlayerRepository.GetPlayerAutoTransferFlagAsync(Arg.Any<string>())
            .Returns(true);

        var actual = await sut.GetAsync(query);

        Assert.IsTrue(actual);
    }

    [Test]
    public async Task Can_Toggle_Player_IsAutoTransferToGame()
    {
        var fakePlayerRepository = Substitute.For<IPlayerRepository>();

        var sut = new TogglePlayerIsAutoTransferToGameCommandService(fakePlayerRepository);

        var command = new TogglePlayerIsAutoTransferToGameCommand("ThisIsPlayerCode");

        await sut.ExecuteAsync(command);

        _ = fakePlayerRepository.Received(1)
            .TogglePlayerAutoTransferFlagAsync(Arg.Is(command.PlayerCode));
    }

    [Test]
    public void Not_Found_PlayerGameSetting_Expression_Should_Be_True()
    {
        var sut = default(PlayerGameSetting);

        var actual = sut?.IsAutoTransferToGame ?? true;

        Assert.IsTrue(actual);
    }
}