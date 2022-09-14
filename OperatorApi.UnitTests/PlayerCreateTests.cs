using System;
using System.Threading.Tasks;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using Tgs.BrandCertificate.Clients;
using Tgs.Core.Infrastructures.Security;
using Tgs.PlayerService.Client;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Apis.OperatorApi.Validators;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Core.Models.Exceptions;
using TGS.Services.Operations;
using TGS.Services.Operations.Models.Services;
using TGS.Services.Operations.Validators;
using TGS.Services.Operator.ApplicationServices.Player;
using TGS.Services.Operator.PlayerRegisterFlow;

namespace TGS.Tests.OperatorApi.UnitTests;

public class PlayerCreateTests
{
    [Test]
    public async Task Get_Player_Current_Register_Step()
    {
        var fakeBrandRegisterFlowDefinitionManager = Substitute.For<IBrandRegisterFlowDefinitionManager>();

        var sut = new GetCurrentPlayerRegisterFlowStepQueryService(
            fakeBrandRegisterFlowDefinitionManager);

        var query = new GetCurrentPlayerRegisterFlowStepQuery(Guid.NewGuid(), null);

        var flowDefinition = new RegisterFlowDefinition(NullLogger<RegisterFlow>.Instance);

        var fakeCreatePlayerStep = new StubRegisterFlowStep()
        {
            Type = "AAA"
        };

        fakeCreatePlayerStep.SetProcessInputDelegate((_, _, _) => Task.FromResult(
            new RegisterFlowProcessResult(true, null, null)));

        flowDefinition.AddStep(fakeCreatePlayerStep);

        fakeBrandRegisterFlowDefinitionManager.GetRegisterFlowDefinitionAsync(Arg.Is(query.BrandId))
            .Returns(flowDefinition);

        var actual = await sut.GetAsync(query);

        Assert.AreEqual("AAA", actual);
    }

    [Test]
    public async Task Register_Player_With_Existent_InvitationCode()
    {
        // arrange
        var brandId = Guid.NewGuid();
        var invitationCode = "6rd4mq";

        var fakePlayerOperations = Substitute.For<IPlayerOperations>();
        var fakePasswordHandler = Substitute.For<IPasswordHandler>();
        var fakeAgentOperations = Substitute.For<IAgentOperations>();
        var fakeBrandOperations = Substitute.For<IBrandOperations>();
        var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

        fakeAgentOperations
            .GetAgentByInvitationCodeAsync(
                Arg.Is(brandId),
                Arg.Is(invitationCode))
            .Returns(Task.FromResult(new Agent()));

        fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
            .Returns(ValueTask.FromResult(
                new Tgs.BrandCertificate.Clients.BrandInfo
                {
                    Id = brandId,
                    Code = "code",
                    Currency = "CNY"
                }));

        fakePlayerOperations.CreatePlayerAsync(Arg.Any<CreatePlayerData>())
            .Returns("TestPlayerCode");

        var sut = new CreateAgentInvitationPlayerCommandService(
            fakePlayerOperations,
            fakePasswordHandler,
            fakeBrandOperations,
            fakeAgentOperations,
            fakePlayerServiceClient,
            fakeBrandServiceClient);

        var command = new CreatePlayerWithAgentInvitationCodeCommand(
            brandId,
            "username",
            "password",
            "CNY",
            invitationCode,
            false);

        // act
        var actual = await sut.GetAsync(command);

        // assert
        Assert.IsTrue(actual.IsNew);

        _ = fakePlayerOperations.Received(1)
            .CreatePlayerAsync(Arg.Is<CreatePlayerData>(data => data.BrandId == command.BrandId
                && data.UserName == command.UserName
                && data.PlayerStatus == Core.Models.Enums.PlayerStatus.RegisterUndone
                && data.AgentInvitationCode == command.AgentInvitationCode));
    }

    [Test]
    public async Task Register_Player_With_Existent_Upper_PlayerCode()
    {
        var brandId = Guid.NewGuid();
        var upperPlayerCode = "upper";

        var fakePlayerOperations = Substitute.For<IPlayerOperations>();
        var fakePasswordHandler = Substitute.For<IPasswordHandler>();
        var fakeBrandOperations = Substitute.For<IBrandOperations>();
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
        var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

        fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
            .Returns(ValueTask.FromResult(
                new Tgs.BrandCertificate.Clients.BrandInfo
                {
                    Id = brandId,
                    Code = "code",
                    Currency = "CNY"
                }));

        fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(upperPlayerCode))
            .Returns(new Tgs.PlayerService.Client.PlayerInfo
            {
                PlayerCode = upperPlayerCode,
                BrandId = brandId
            });

        var sut = new CreatePlayerInvitationPlayerCommandService(
            fakePlayerOperations,
            fakePasswordHandler,
            fakeBrandOperations,
            fakePlayerServiceClient,
            fakeBrandServiceClient);

        var command = new CreatePlayerWithPlayerInvitationCommand(
            brandId,
            "username",
            "password",
            "CNY",
            upperPlayerCode,
            false);

        var actual = await sut.GetAsync(command);

        // assert
        Assert.IsTrue(actual.IsNew);

        _ = fakePlayerOperations.Received(1)
            .CreatePlayerAsync(Arg.Is<CreatePlayerData>(data => data.BrandId == command.BrandId
                && data.UserName == command.UserName
                && data.PlayerStatus == Core.Models.Enums.PlayerStatus.RegisterUndone
                && data.UpperPlayerCode == command.UpperPlayerCode));
    }

    [Test]
    public async Task Register_Player_With_No_InvitationCode()
    {
        // arrange
        var brandId = Guid.NewGuid();

        var fakePlayerOperations = Substitute.For<IPlayerOperations>();
        var fakeBrandOperations = Substitute.For<IBrandOperations>();

        var fakePasswordHandler = Substitute.For<IPasswordHandler>();
        var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

        fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
            .Returns(ValueTask.FromResult(
                new Tgs.BrandCertificate.Clients.BrandInfo
                {
                    Id = brandId,
                    Code = "code",
                    Currency = "CNY"
                }));

        var sut = new CreatePlayerCommandService(
            fakePlayerOperations,
            fakePasswordHandler,
            fakeBrandOperations,
            fakePlayerServiceClient,
            fakeBrandServiceClient);

        var password = "password";

        var command = new CreatePlayerCommand(
            brandId,
            "username",
            password,
            "CNY",
            false);

        // act
        var actual = await sut.GetAsync(command);

        // assert
        Assert.IsTrue(actual.IsNew);

        _ = fakePlayerOperations.Received(1)
            .CreatePlayerAsync(Arg.Is<CreatePlayerData>(data => data.BrandId == command.BrandId
                && data.UserName == command.UserName
                && data.PlayerStatus == Core.Models.Enums.PlayerStatus.RegisterUndone));
    }

    [Test]
    public void Register_Player_With_Non_Existent_InvitationCode()
    {
        // arrange
        var brandId = Guid.NewGuid();

        var fakePlayerOperations = Substitute.For<IPlayerOperations>();
        var fakeBrandOperations = Substitute.For<IBrandOperations>();
        var fakePasswordHandler = Substitute.For<IPasswordHandler>();
        var fakeAgentOperations = Substitute.For<IAgentOperations>();
        var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

        fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
            .Returns(ValueTask.FromResult(
                new Tgs.BrandCertificate.Clients.BrandInfo
                {
                    Id = brandId,
                    Code = "code",
                    Currency = "CNY"
                }));

        var sut = new CreateAgentInvitationPlayerCommandService(
            fakePlayerOperations,
            fakePasswordHandler,
            fakeBrandOperations,
            fakeAgentOperations,
            fakePlayerServiceClient,
            fakeBrandServiceClient);

        var password = "password";
        var invitationCode = "NonExistentCode";

        var query = new CreatePlayerWithAgentInvitationCodeCommand(
            brandId,
            "username",
            password,
            "CNY",
            invitationCode,
            false);

        Assert.ThrowsAsync<InvalidInvitationCodeException>(async () => await sut.GetAsync(query));
    }

    [Test]
    public void Register_Player_With_Non_Existent_Upper_PlayerCode()
    {
        var brandId = Guid.NewGuid();
        var upperPlayerCode = "upper";

        var fakePlayerOperations = Substitute.For<IPlayerOperations>();
        var fakePasswordHandler = Substitute.For<IPasswordHandler>();
        var fakeBrandOperations = Substitute.For<IBrandOperations>();
        var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

        fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
            .Returns(ValueTask.FromResult(
                new Tgs.BrandCertificate.Clients.BrandInfo
                {
                    Id = brandId,
                    Code = "code",
                    Currency = "CNY"
                }));

        var sut = new CreatePlayerInvitationPlayerCommandService(
            fakePlayerOperations,
            fakePasswordHandler,
            fakeBrandOperations,
            fakePlayerServiceClient,
            fakeBrandServiceClient);

        var command = new CreatePlayerWithPlayerInvitationCommand(
            brandId,
            "username",
            "password",
            "CNY",
            upperPlayerCode,
            false);

        Assert.ThrowsAsync<InvalidInvitationCodeException>(async () => await sut.GetAsync(command));
    }

    [Test]
    public void Register_Player_With_Other_Brand_InvitationCode_Should_Throw_Error()
    {
        var brandId = Guid.NewGuid();
        var invitationCode = "6rd4mq";

        var fakeStatusValidator = Substitute.For<IStatusValidator>();
        var fakePlayerOperations = Substitute.For<IPlayerOperations>();
        var fakePasswordHandler = Substitute.For<IPasswordHandler>();
        var fakeAgentOperations = Substitute.For<IAgentOperations>();
        var fakeBrandOperations = Substitute.For<IBrandOperations>();
        var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

        fakeAgentOperations
            .GetAgentByInvitationCodeAsync(
                Arg.Is(brandId),
                Arg.Is(invitationCode))
            .Returns(Task.FromResult(default(Agent)));

        fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
            .Returns(ValueTask.FromResult(
                new Tgs.BrandCertificate.Clients.BrandInfo
                {
                    Id = brandId,
                    Code = "code",
                    Currency = "CNY"
                }));

        var sut = new CreateAgentInvitationPlayerCommandService(
            fakePlayerOperations,
            fakePasswordHandler,
            fakeBrandOperations,
            fakeAgentOperations,
            fakePlayerServiceClient,
            fakeBrandServiceClient);

        var query = new CreatePlayerWithAgentInvitationCodeCommand(
            brandId,
            "username",
            "password",
            "CNY",
            invitationCode,
            false);

        // act
        Assert.ThrowsAsync<InvalidInvitationCodeException>(
            async () => await sut.GetAsync(query));
    }

    [Test]
    public void Register_Player_With_Other_Brand_Upper_PlayerCode_Should_Throw_Error()
    {
        var brandId = Guid.NewGuid();
        var upperPlayerCode = "upper";

        var fakePlayerOperations = Substitute.For<IPlayerOperations>();
        var fakePasswordHandler = Substitute.For<IPasswordHandler>();
        var fakeBrandOperations = Substitute.For<IBrandOperations>();
        var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

        fakeBrandServiceClient.GetBrandInfoAsync(Arg.Is(brandId))
            .Returns(ValueTask.FromResult(
                new Tgs.BrandCertificate.Clients.BrandInfo
                {
                    Id = brandId,
                    Code = "code",
                    Currency = "CNY"
                }));

        fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(upperPlayerCode))
            .Returns(new Tgs.PlayerService.Client.PlayerInfo
            {
                PlayerCode = upperPlayerCode,
                BrandId = Guid.NewGuid()
            });

        var sut = new CreatePlayerInvitationPlayerCommandService(
            fakePlayerOperations,
            fakePasswordHandler,
            fakeBrandOperations,
            fakePlayerServiceClient,
            fakeBrandServiceClient);

        var query = new CreatePlayerWithPlayerInvitationCommand(
            brandId,
            "username",
            "password",
            "CNY",
            upperPlayerCode,
            false);

        // act
        Assert.ThrowsAsync<InvalidInvitationCodeException>(
            async () => await sut.GetAsync(query));
    }

    [Test]
    public void Verify_Player_Account_Input_Max_Length()
    {
        // arrange
        var sut = new CreatePlayerRequestValidator();

        var request = new CreatePlayerRequest
        {
            Password = "123456",
            UserName = "123456789012345678901234567890123",
            AgentInvitationCode = string.Empty,
            IsTestPlayer = false
        };

        // act
        var actual = sut.TestValidate(request);
        // assert
        actual.ShouldHaveValidationErrorFor(request => request.UserName);
    }

    [Test]
    public void Verify_Player_Account_Input_Min_Length()
    {
        // arrange
        var sut = new CreatePlayerRequestValidator();

        var request = new CreatePlayerRequest
        {
            Password = "123456",
            UserName = "user",
            AgentInvitationCode = string.Empty,
            IsTestPlayer = false
        };

        // act
        var actual = sut.TestValidate(request);

        // assert
        actual.ShouldHaveValidationErrorFor(request => request.UserName);
    }

    [Test]
    public void Verify_Player_Account_With_Special_Character()
    {
        // arrange
        var sut = new CreatePlayerRequestValidator();

        var request = new CreatePlayerRequest
        {
            Password = "123456",
            UserName = "abc123_",
            AgentInvitationCode = string.Empty,
            IsTestPlayer = false
        };

        // act
        var actual = sut.TestValidate(request);
        // assert
        actual.ShouldHaveValidationErrorFor(request => request.UserName);
    }

    [Test]
    public void Verify_Player_Password_Input_Max_Length()
    {
        // arrange
        var sut = new CreatePlayerRequestValidator();

        var request = new CreatePlayerRequest
        {
            Password = "123456789012345678901",
            UserName = "username",
            AgentInvitationCode = string.Empty,
            IsTestPlayer = false
        };

        // act
        var actual = sut.TestValidate(request);
        // assert
        actual.ShouldHaveValidationErrorFor(request => request.Password);
    }

    [Test]
    public void Verify_Player_Password_Input_Min_Length()
    {
        // arrange
        var sut = new CreatePlayerRequestValidator();

        var request = new CreatePlayerRequest
        {
            Password = "1234",
            UserName = "username",
            AgentInvitationCode = string.Empty,
            IsTestPlayer = false
        };

        // act
        var actual = sut.TestValidate(request);
        // assert
        actual.ShouldHaveValidationErrorFor(request => request.Password);
    }
}
