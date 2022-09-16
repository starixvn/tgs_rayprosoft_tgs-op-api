using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;
using NUnit.Framework;
using Tgs.BrandCertificate.Clients;
using Tgs.Core.Infrastructures.Security;
using Tgs.PlayerService.Client;
using TGS.Core.DataServices.Tgs;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Core.Models.Tokens;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.Player;
using TGS.Services.Operator.ValidateCode;
using TGS.SmsService.Client;

namespace TGS.Tests.OperatorApi.UnitTests;

public class PlayerPasswordTests
{
    [Test]
    public async Task Change_Player_Password_Should_Success()
    {
        var playerCode = "testPlayerCode";
        var fakeHttpContext = new DefaultHttpContext();

        fakeHttpContext.Items.Add("playerToken",
            new PlayerTokenData
            {
                PlayerCode = playerCode
            });

        var fakePlayerRepository = Substitute.For<IPlayerRepository>();

        _ = fakePlayerRepository
              .GetPlayerLoginPasswordInfoAsync(Arg.Any<string>())
              .Returns(new PasswordInfo(
                  Salt: new byte[]
                  {
                    1,
                    2,
                    3
                  },
                  Password: new byte[]
                  {
                    4,
                    5,
                    6
                  }));

        var fakePasswordHandler = Substitute.For<IPasswordHandler>();

        var sut = new ChangePasswordQueryService(
            fakePlayerRepository,
            fakePasswordHandler);

        var brandId = Guid.NewGuid();
        var query = new ChangePasswordQuery(brandId, playerCode, "oldPassword", "newPassword");

        _ = fakePasswordHandler
            .Verify(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Is(query.OldPassword))
            .Returns(true);

        _ = fakePasswordHandler
             .GenerateCredential(Arg.Is(query.NewPassword))
             .Returns((
                 Salt: new byte[]
                 {
                    1,
                    2,
                    3
                 },
                 HashedPassword: new byte[]
                 {
                    4,
                    5,
                    6
                 }));

        var expected = playerCode;

        var actual = await sut.GetAsync(query);

        Assert.AreEqual(expected, actual.PlayerCode);

        _ = fakePlayerRepository
            .Received(1)
            .UpdatePlayerLoginPasswordAsync(
                Arg.Is(playerCode),
                Arg.Is<byte[]>(x => x.Length > 0),
                Arg.Is<byte[]>(x => x.Length > 0));
    }

    [Test]
    public async Task Forgot_Password_By_Sms_Will_Send_Message()
    {
        var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
        var fakeValidateCodeGenerator = Substitute.For<IValidateCodeGenerator>();
        var fakeValidateCodeOperator = Substitute.For<IValidateCodeOperator>();
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
        var fakeSmsServiceClient = Substitute.For<ISmsServiceClient>();

        var sut = new ForgotPasswordBySmsCommandService(
            fakePlayerServiceClient,
            fakeValidateCodeGenerator,
            fakeValidateCodeOperator,
            fakeBrandServiceClient,
            fakeSmsServiceClient);

        var command = new ForgotPasswordBySmsCommand(
            Guid.NewGuid(),
            CultureInfo.GetCultureInfo("en-US"),
            "886",
            "0912345678");

        fakePlayerServiceClient
            .GetPlayerInfoByPhoneAsync(
                Arg.Is(command.BrandId),
                Arg.Is(command.AreaCode),
                Arg.Is(command.Phone))
            .Returns(new Tgs.PlayerService.Client.PlayerInfo
            {
                PlayerCode = "TestCode",
                BrandId = command.BrandId,
                PhoneAuthentication = true
            });

        var validateCode = "123456";
        var token = "abc123";

        fakeValidateCodeGenerator.GenerateCode().Returns(validateCode);
        fakeValidateCodeGenerator.GenerateSecurityKey().Returns(token);

        fakeValidateCodeOperator.SaveValidateDataAsync(
            Arg.Is(validateCode),
            Arg.Is(token),
            Arg.Any<ValidateCodeData>())
            .Returns(true);

        fakeBrandServiceClient
            .GetBrandInfoAsync(Arg.Is(command.BrandId))
            .Returns(new Tgs.BrandCertificate.Clients.BrandInfo
            {
                Id = command.BrandId,
                Code = "brandCode",
                Currency = "CNY",
                SmsEnabled = true,
                SmsRegisterFlowEnabled = true
            });

        var expected = $"Your ID verification code is : {validateCode}";

        await sut.GetAsync(command);

        _ = fakeSmsServiceClient.Received(1)
            .SendShortMessageAsync(
                Arg.Is($"{command.AreaCode}{command.Phone}"),
                Arg.Is(expected),
                Arg.Is(command.BrandId));
    }

    [Test]
    public void Forgot_Password_Not_Found_Player_By_Phone_Will_Throw_Exception()
    {
        var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
        var fakeValidateCodeGenerator = Substitute.For<IValidateCodeGenerator>();
        var fakeValidateCodeOperator = Substitute.For<IValidateCodeOperator>();
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
        var fakeSmsServiceClient = Substitute.For<ISmsServiceClient>();

        var sut = new ForgotPasswordBySmsCommandService(
            fakePlayerServiceClient,
            fakeValidateCodeGenerator,
            fakeValidateCodeOperator,
            fakeBrandServiceClient,
            fakeSmsServiceClient);

        var command = new ForgotPasswordBySmsCommand(
            Guid.NewGuid(),
            CultureInfo.GetCultureInfo("en-US"),
            "886",
            "0912345678");

        Assert.ThrowsAsync<PlayerNotFoundException>(async () => await sut.GetAsync(command));
    }

    [Test]
    public async Task Forgot_Password_Will_Sent_Email()
    {
        var email = "email@test.com";

        var dbContextOptionsBuilder = new DbContextOptionsBuilder<TgsRepository>();

        dbContextOptionsBuilder
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));

        var fakeTgsRepository = new TgsRepository(dbContextOptionsBuilder.Options);

        var player = new Player
        {
            PlayerCode = "abcdef123456",
            Username = "Tester",
            BrandId = Guid.NewGuid(),
            Email = email,
            PhoneAuthentication = true,
            DefaultWalletCurrencyCode = "CNY",
            Password = Array.Empty<byte>(),
            Salt = Array.Empty<byte>(),
            WithdrawPassword = Array.Empty<byte>(),
            WithdrawSalt = Array.Empty<byte>()
        };

        await fakeTgsRepository.Players.AddAsync(player);
        await fakeTgsRepository.SaveChangesAsync();

        var fakeValidateCodeGenerator = Substitute.For<IValidateCodeGenerator>();
        var fakeValidateCodeOperator = Substitute.For<IValidateCodeOperator>();

        var sut = new ForgotPasswordByMailCommandService(
            fakeTgsRepository,
            fakeValidateCodeGenerator,
            fakeValidateCodeOperator);

        var command = new ForgotPasswordByMailCommand(
            player.BrandId,
            CultureInfo.GetCultureInfo("en-US"),
            email);

        var validateCode = "123456";
        var token = "abc123";

        fakeValidateCodeGenerator.GenerateCode().Returns(validateCode);
        fakeValidateCodeGenerator.GenerateSecurityKey().Returns(token);

        fakeValidateCodeOperator
            .SaveValidateDataAsync(
                Arg.Is(validateCode),
                Arg.Is(token),
                Arg.Any<ValidateCodeData>())
            .Returns(true);

        await sut.GetAsync(command);

        _ = fakeValidateCodeOperator
            .Received(1)
            .SendEmailAsync(
                Arg.Is(command.CultureInfo),
                Arg.Is(player.Email),
                Arg.Is(validateCode));
    }

    [Test]
    public void Reset_Password_If_Not_Verify_ValidateCode_Should_Throw_Exception()
    {
        var fakePlayerRepository = Substitute.For<IPlayerRepository>();
        var fakeValidateCodeOperator = Substitute.For<IValidateCodeOperator>();

        var sut = new ResetPasswordCommandService(
            fakePlayerRepository,
            Substitute.For<IPasswordHandler>(),
            fakeValidateCodeOperator);

        var command = new ResetPasswordCommand("31B51623-2D9D-4A5B-B6F4-32924C5A91EB", "654321", "newPassword");

        _ = fakeValidateCodeOperator.GetValidateCodeDataAsync<ValidateCodeData>(Arg.Any<string>(), Arg.Any<string>())
             .Returns((ValidateCodeData)null);

        _ = Assert.ThrowsAsync<ValidateCodeNotVerifyException>(async () => await sut.ExecuteAsync(command));
    }

    [Test]
    public async Task Reset_Password_Should_Success()
    {
        var fakePlayerRepository = Substitute.For<IPlayerRepository>();
        var fakeValidateCodeOperator = Substitute.For<IValidateCodeOperator>();

        var sut = new ResetPasswordCommandService(
            fakePlayerRepository,
            Substitute.For<IPasswordHandler>(),
            fakeValidateCodeOperator);

        var command = new ResetPasswordCommand("31B51623-2D9D-4A5B-B6F4-32924C5A91EB", "123456", "newPassword");

        _ = fakeValidateCodeOperator.GetValidateCodeDataAsync<ValidateCodeData>(Arg.Any<string>(), Arg.Any<string>())
              .Returns(callInfo =>
              {
                  var token = callInfo.ArgAt<string>(0);

                  return new ValidateCodeData("testPlayerCode", token);
              });

        await sut.ExecuteAsync(command);

        _ = fakePlayerRepository
            .Received(1)
            .UpdatePlayerLoginPasswordAsync(
                Arg.Is("testPlayerCode"),
            Arg.Any<byte[]>(),
                Arg.Any<byte[]>());
    }

    [Test]
    public async Task Validate_Password_Should_Success()
    {
        var playerCode = "playerCode";
        var password = "password";

        var fakePlayerRepository = Substitute.For<IPlayerRepository>();
        var fakePasswordHandler = Substitute.For<IPasswordHandler>();

        _ = fakePlayerRepository.GetPlayerLoginPasswordInfoAsync(Arg.Any<string>())
            .Returns(new PasswordInfo(
                Array.Empty<byte>(),
                Array.Empty<byte>()
            ));

        var sut = new PlayerPasswordValidateCommandService(fakePlayerRepository, fakePasswordHandler);

        var command = new PlayerPasswordValidateCommand(playerCode, password);

        await sut.GetAsync(command);

        _ = fakePasswordHandler
            .Received(1)
            .Verify(Arg.Any<byte[]>(), Arg.Any<byte[]>(), Arg.Any<string>());
    }
}
