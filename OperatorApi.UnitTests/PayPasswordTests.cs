using System;
using System.Globalization;
using System.Threading.Tasks;
using ExpectedObjects;
using FluentValidation.TestHelper;
using NSubstitute;
using NUnit.Framework;
using Tgs.BrandCertificate.Clients;
using Tgs.Core.Infrastructures.Security;
using TGS.Apis.OperatorApi.Models.Messages;
using TGS.Apis.OperatorApi.Validators;
using TGS.Core.DataServices.Tgs.Models;
using TGS.Core.Infrastructures.Mail;
using TGS.Services.Operations;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.PayPassowrd;
using TGS.Services.Operator.ValidateCode;
using TGS.SmsService.Client;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class PayPasswordTests
{
    [Test]
    public void Change_Pay_Password_If_Not_Verify_ValidateCode_Should_Throw_Exception()
    {
        var fakePlayerOperations = Substitute.For<IPlayerOperations>();
        var fakePasswordHandler = Substitute.For<IPasswordHandler>();
        var fakeValidateCodeOperator = Substitute.For<IValidateCodeOperator>();

        var sut = new ChangePayPasswordCommandService(
            fakePlayerOperations,
            fakePasswordHandler,
            fakeValidateCodeOperator);

        var player = new Player
        {
            PlayerCode = "abcdef123456",
            Username = "Tester",
            BrandId = Guid.NewGuid(),
            Phone = "0988123456",
            PhoneAuthentication = true,
            DefaultWalletCurrencyCode = "CNY"
        };

        var newPassword = "newPassword";
        var validateCode = "123456";
        var token = "99CFACE4-03FE-42A2-B1DB-57C93A7BCD2E";

        var command = new ChangePayPasswordCommand(
            player.PlayerCode,
            newPassword,
            validateCode,
            token);

        fakePlayerOperations.GetPlayerByCodeAsync(Arg.Is(player.PlayerCode)).Returns(player);

        fakeValidateCodeOperator
            .GetValidateCodeDataAsync<ValidateCodeData>(Arg.Is(validateCode), Arg.Is(token))
            .Returns((ValidateCodeData)null);

        Assert.ThrowsAsync<ValidateCodeNotVerifyException>(async () => await sut.ExecuteAsync(command));
    }

    [Test]
    public async Task Change_Pay_Password_Should_Success()
    {
        var fakePlayerOperations = Substitute.For<IPlayerOperations>();
        var fakePasswordHandler = Substitute.For<IPasswordHandler>();
        var fakeValidateCodeOperator = Substitute.For<IValidateCodeOperator>();

        var sut = new ChangePayPasswordCommandService(
            fakePlayerOperations,
            fakePasswordHandler,
            fakeValidateCodeOperator);

        var player = new Player
        {
            PlayerCode = "abcdef123456",
            Username = "Tester",
            BrandId = Guid.NewGuid(),
            Phone = "0988123456",
            PhoneAuthentication = true,
            DefaultWalletCurrencyCode = "CNY"
        };

        var newPassword = "newPassword";
        var validateCode = "123456";
        var token = "99CFACE4-03FE-42A2-B1DB-57C93A7BCD2E";

        var command = new ChangePayPasswordCommand(
            player.PlayerCode,
            newPassword,
            validateCode,
            token);

        fakePlayerOperations.GetPlayerByCodeAsync(Arg.Is(player.PlayerCode)).Returns(player);

        fakeValidateCodeOperator
            .GetValidateCodeDataAsync<ValidateCodeData>(Arg.Is(validateCode), Arg.Is(token))
            .Returns(new ValidateCodeData(player.PlayerCode, token));

        await sut.ExecuteAsync(command);

        fakePasswordHandler.Received(1).GenerateCredential(Arg.Is(newPassword));

        _ = fakePlayerOperations
            .Received(1)
            .SetPayPasswordAsync(Arg.Is(player.PlayerCode),
                Arg.Any<byte[]>(),
                Arg.Any<byte[]>(),
                Arg.Any<DateTimeOffset>());
    }

    [Test]
    public void If_Already_Set_Pay_Password_Will_Throw_Exception()
    {
        var fakePlayerOperations = Substitute.For<IPlayerOperations>();
        var fakePasswordHandler = Substitute.For<IPasswordHandler>();

        var sut = new SetFirstPayPasswordCommandService(
            fakePlayerOperations,
            fakePasswordHandler);

        var playerCode = "testPlayerCode";
        var password = "123456";
        var command = new SetFirstPayPasswordCommand(playerCode, password);

        fakePlayerOperations
            .GetPlayerByCodeAsync(Arg.Is(playerCode))
            .Returns(new Player
            {
                PlayerCode = playerCode,
                Username = "Tester",
                WithdrawPassword = new byte[]
                {
                    1,
                    2,
                    3
                }
            });

        Assert.ThrowsAsync<PayPasswordAlreadySetException>(async () => await sut.ExecuteAsync(command));
    }

    [Test]
    public async Task Set_Pay_Password_Should_Success()
    {
        var fakePlayerOperations = Substitute.For<IPlayerOperations>();
        var fakePasswordHandler = Substitute.For<IPasswordHandler>();

        var sut = new SetFirstPayPasswordCommandService(
            fakePlayerOperations,
            fakePasswordHandler);

        var playerCode = "testPlayerCode";
        var password = "123456";
        var command = new SetFirstPayPasswordCommand(playerCode, password);

        fakePlayerOperations
            .GetPlayerByCodeAsync(Arg.Is(playerCode))
            .Returns(new Player
            {
                PlayerCode = playerCode,
                Username = "Tester",
                WithdrawPassword = Array.Empty<byte>()
            });

        await sut.ExecuteAsync(command);

        fakePasswordHandler.Received(1).GenerateCredential(Arg.Is(password));

        _ = fakePlayerOperations
            .Received(1)
            .SetPayPasswordAsync(Arg.Is(playerCode),
                Arg.Any<byte[]>(),
                Arg.Any<byte[]>(),
                Arg.Any<DateTimeOffset>());
    }

    [Test]
    public async Task Start_Change_Pay_Password_Should_Send_Confirm_Email()
    {
        var fakePlayerOperations = Substitute.For<IPlayerOperations>();
        var fakeValidateCodeGenerator = Substitute.For<IValidateCodeGenerator>();
        var fakeValidateCodeOperator = Substitute.For<IValidateCodeOperator>();
        var fakeEmailClient = Substitute.For<IEmailClient>();
        var fakeSmsServiceClient = Substitute.For<ISmsServiceClient>();
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

        var sut = new StartChangePayPasswordCommandService(
            fakePlayerOperations,
            fakeValidateCodeGenerator,
            fakeValidateCodeOperator,
            fakeEmailClient,
            fakeSmsServiceClient,
            fakeBrandServiceClient);

        var email = "email@test.com";

        var player = new Player
        {
            PlayerCode = "abcdef123456",
            Username = "Tester",
            BrandId = Guid.NewGuid(),
            Email = email,
            DefaultWalletCurrencyCode = "CNY"
        };

        var command = new StartChangePayPasswordCommand(
            player.PlayerCode,
            CultureInfo.GetCultureInfo("en-US"));

        var validateCode = "123456";
        var expectedSecurityKey = "3F8E5681-50C8-4F2F-8379-3AA6EF2C6064";

        fakePlayerOperations.GetPlayerByCodeAsync(Arg.Is(player.PlayerCode))
            .Returns(player);

        fakeValidateCodeGenerator.GenerateCode()
            .Returns(validateCode);

        fakeValidateCodeGenerator.GenerateSecurityKey()
            .Returns(expectedSecurityKey);

        fakeValidateCodeOperator
            .SaveValidateDataAsync(Arg.Is(validateCode), Arg.Is(expectedSecurityKey), Arg.Any<ValidateCodeData>())
            .Returns(true);

        var expected = new
        {
            Receivers = new[]
            {
                email
            },
            Subject = "Verification Code",
            Content = $"Your ID verification code is : {validateCode}"
        }.ToExpectedObject();

        SendEmailRequest actual = null;

        fakeEmailClient
            .SendAsync(Arg.Do<SendEmailRequest>(msg =>
            {
                actual = msg;
            }))
            .Returns(ValueTask.FromResult(new SendEmailResponse()));

        fakeBrandServiceClient.GetBrandInfoAsync(Arg.Any<Guid>())
            .Returns(new Tgs.BrandCertificate.Clients.BrandInfo
            {
                Id = player.BrandId,
                Code = "brandCode",
                Currency = "CNY"
            });

        var actualSecurityKey = await sut.GetAsync(command);

        _ = fakeEmailClient
            .Received(1)
            .SendAsync(Arg.Any<SendEmailRequest>());

        expected.ShouldMatch(actual);
        Assert.AreEqual(expectedSecurityKey, actualSecurityKey);
    }

    [Test]
    public void Start_Change_Pay_Password_Should_Verify_Email()
    {
        var fakePlayerOperations = Substitute.For<IPlayerOperations>();
        var playerCode = "abcdef123456";

        var player = new Player
        {
            PlayerCode = playerCode,
            Username = "Tester",
            BrandId = Guid.NewGuid(),
            DefaultWalletCurrencyCode = "CNY"
        };
        fakePlayerOperations.GetPlayerByCodeAsync(playerCode).Returns(player);
        var fakeValidateCodeGenerator = Substitute.For<IValidateCodeGenerator>();
        var fakeValidateCodeOperator = Substitute.For<IValidateCodeOperator>();
        var fakeEmailClient = Substitute.For<IEmailClient>();
        var fakeSmsServiceClient = Substitute.For<ISmsServiceClient>();
        var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

        var command = new StartChangePayPasswordCommand(
            player.PlayerCode,
            CultureInfo.GetCultureInfo("en-US"));

        var sut = new StartChangePayPasswordCommandService(
            fakePlayerOperations,
            fakeValidateCodeGenerator,
            fakeValidateCodeOperator,
            fakeEmailClient,
            fakeSmsServiceClient,
            fakeBrandServiceClient);

        Assert.ThrowsAsync<EmailNotVerifyException>(async () => await sut.GetAsync(command));
    }

    [Test]
    public void Verify_Change_Pay_Password_Input_Max_Length()
    {
        // arrange
        var sut = new ChangePayPasswordRequestValidator();

        var request = new ChangePayPasswordRequest
        {
            NewPassword = "123456789012345678901"
        };

        // act
        var actual = sut.TestValidate(request);

        // assert
        actual.ShouldHaveValidationErrorFor(request => request.NewPassword);
    }

    [Test]
    public void Verify_Change_Pay_Password_Input_Min_Length()
    {
        // arrange
        var sut = new ChangePayPasswordRequestValidator();

        var request = new ChangePayPasswordRequest
        {
            NewPassword = "12345"
        };

        // act
        var actual = sut.TestValidate(request);

        // assert
        actual.ShouldHaveValidationErrorFor(request => request.NewPassword);
    }

    [Test]
    public void Verify_Change_Pay_Password_Input_With_Empty()
    {
        // arrange
        var sut = new ChangePayPasswordRequestValidator();

        var request = new ChangePayPasswordRequest
        {
            NewPassword = string.Empty
        };

        // act
        var actual = sut.TestValidate(request);

        // assert
        actual.ShouldHaveValidationErrorFor(request => request.NewPassword);
    }

    [Test]
    public void Verify_Set_Pay_Password_Input_Max_Length()
    {
        // arrange
        var sut = new SetPayPasswordRequestValidator();

        var request = new SetPayPasswordRequest
        {
            Password = "123456789012345678901"
        };

        // act
        var actual = sut.TestValidate(request);

        // assert
        actual.ShouldHaveValidationErrorFor(request => request.Password);
    }

    [Test]
    public void Verify_Set_Pay_Password_Input_Min_Length()
    {
        // arrange
        var sut = new SetPayPasswordRequestValidator();

        var request = new SetPayPasswordRequest
        {
            Password = "12345"
        };

        // act
        var actual = sut.TestValidate(request);

        // assert
        actual.ShouldHaveValidationErrorFor(request => request.Password);
    }

    [Test]
    public void Verify_Set_Pay_Password_Input_With_Empty()
    {
        // arrange
        var sut = new SetPayPasswordRequestValidator();

        var request = new SetPayPasswordRequest
        {
            Password = string.Empty
        };

        // act
        var actual = sut.TestValidate(request);

        // assert
        actual.ShouldHaveValidationErrorFor(request => request.Password);
    }
}
