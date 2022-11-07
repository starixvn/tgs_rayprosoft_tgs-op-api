using System;
using System.Text.Json;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Tgs.Payment.Clients;
using TGS.Core.Infrastructures.Redis;
using TGS.Core.Models.Enums;
using TGS.Services.Operator.ApplicationServices.Withdraw;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class BankInfoTests
{
	[Test]
	public async Task Player_Can_Create_BankInfo()
	{
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

		var sut = new CreateBankInfoCommandService(fakePaymentServiceClient);

		var brandId = Guid.NewGuid();
		var playerCode = "ThisIsPlayerCode";
		var paymentChannelId = Guid.NewGuid();
		var bankType = "Transfer";
		var detail = new
		{
			bankAccount = "0987654321"
		};

		var command = new CreateBankInfoCommand(
			brandId,
			playerCode,
			paymentChannelId,
			bankType,
			JsonSerializer.SerializeToDocument(detail));

		await sut.ExecuteAsync(command);

		_ = fakePaymentServiceClient
			.Received(1)
			.CreatePlayerBankInfoAsync(
				Arg.Is(brandId),
				Arg.Is(playerCode),
				Arg.Is<BankCreateInfo>(info => info.PaymentChannelId == paymentChannelId));
	}

	[Test]
	public async Task Player_Can_Delete_BankInfo()
	{
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

		var sut = new DeleteBankInfoCommandService(fakePaymentServiceClient);

		var playerCode = "ThisIsPlayerCode";
		var bankInfoId = Guid.NewGuid();
		var command = new DeleteBankInfoCommand(playerCode, bankInfoId);

		await sut.ExecuteAsync(command);

		_ = fakePaymentServiceClient
			.Received(1)
			.DeletePlayerBankInfoAsync(Arg.Is(playerCode), Arg.Is(bankInfoId));
	}

	[Test]
	public void Player_Can_List_Self_BankInfos()
	{
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

		var sut = new GetPlayerBankInfosQueryService(fakePaymentServiceClient);

		var playerCode = "ThisIsPlayerCode";

		_ = sut.QueryAsync(new GetPlayerBankInfosQuery(playerCode));

		_ = fakePaymentServiceClient.Received(1)
			.GetPlayerBankInfosAsync(Arg.Is(playerCode));
	}

	[Test]
	public async Task Player_Init_BankInfo_If_Brand_Use_BankInfoRegisterFlow_Should_Success()
	{
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakeRedisCacheManager = Substitute.For<IRedisCacheManager>();

		var sut = new CreateInitBankInfoCommandService(
			fakePaymentServiceClient,
			fakeRedisCacheManager);

		var playerCode = "ThisIsPlayerCode";
		var paymentChannelId = Guid.NewGuid();
		var bankType = "Transfer";
		var detail = new
		{
			bankAccount = "0987654321"
		};

		var bankInfoId = Guid.NewGuid();

		var brandInfo = new Tgs.BrandCertificate.Clients.BrandInfo
		{
			Id = Guid.NewGuid(),
			Code = "code",
			Currency = "CNY",
			BankInfoRegisterFlowEnabled = true
		};

		_ = fakePaymentServiceClient.CreatePlayerBankInfoAsync(
			Arg.Is(brandInfo.Id),
			Arg.Is(playerCode),
			Arg.Is<BankCreateInfo>(info => info.PaymentChannelId == paymentChannelId))
			.Returns(bankInfoId);

		var command = new CreateInitBankInfoCommand(
			brandInfo.Id,
			playerCode,
			paymentChannelId,
			bankType,
			JsonSerializer.SerializeToDocument(detail));

		await sut.ExecuteAsync(command);

		_ = fakePaymentServiceClient.Received(1)
			.CreatePlayerBankInfoAsync(
				Arg.Is(brandInfo.Id),
				Arg.Is(playerCode),
				Arg.Is<BankCreateInfo>(info => info.PaymentChannelId == paymentChannelId));

		_ = fakePaymentServiceClient.Received(1)
			.SetDefaultPlayerBankInfoAsync(
				Arg.Is(playerCode),
				Arg.Is(bankInfoId),
				Arg.Is(playerCode));

		_ = fakeRedisCacheManager.Received(1)
			.ClearCacheAsync(
				Arg.Is<object>(x => x.ToString() == nameof(RedisCacheType.Player)),
				Arg.Is<string>(x => x == playerCode),
				Arg.Any<bool>());
	}
}
