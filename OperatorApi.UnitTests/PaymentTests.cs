using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NSubstitute;
using NUnit.Framework;
using Tgs.Payment.Clients;
using Tgs.PlayerService.Client;
using TGS.Services.Operator.ApplicationServices.BrandPayments;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class PaymentTests
{
    [Test]
    public async Task Get_Payment_Channels_Should_Success()
    {
        var moqPaymentServiceClient = new Mock<IPaymentServiceClient>();

        var sut = new AllPaymentChannelsQueryService(moqPaymentServiceClient.Object);

        var query = new AllPaymentChannelsQuery();

        moqPaymentServiceClient
            .Setup(fake => fake.GetAllPaymentChannelsAsync(It.IsAny<CancellationToken>()))
            .Returns(new[] {
                new PaymentChannelInfo
                {
                    Id = Guid.NewGuid(),
                    Name = "test1"
                },
                new PaymentChannelInfo
                {
                    Id = Guid.NewGuid(),
                    Name = "test2"
                }
            }.ToAsyncEnumerable());

        _ = await sut.GetAsync(query);

        moqPaymentServiceClient.Verify(
            fake => fake.GetAllPaymentChannelsAsync(It.IsAny<CancellationToken>()),
            Times.Exactly(1));
    }

    [Test]
    public async Task Get_Payment_Methods_Should_Success()
    {
        var moqPaymentServiceClient = new Mock<IPaymentServiceClient>();

        var sut = new AllPaymentMethodsQueryService(moqPaymentServiceClient.Object);

        var query = new AllPaymentMethodsQuery();

        moqPaymentServiceClient
            .Setup(fake => fake.GetAllPaymentMethodsAsync(It.IsAny<CancellationToken>()))
            .Returns(new[] {
                new PaymentMethodInfo
                {
                    Id = Guid.NewGuid(),
                    Name = "test1",
                    BankType = "Bank"
                },
                new PaymentMethodInfo
                {
                    Id = Guid.NewGuid(),
                    Name = "test2",
                    BankType = "Crypto"
                }
            }.ToAsyncEnumerable());

        _ = await sut.GetAsync(query);

        moqPaymentServiceClient.Verify(
            fake => fake.GetAllPaymentMethodsAsync(It.IsAny<CancellationToken>()),
            Times.Exactly(1));
    }

    [Test]
    public async Task User_Can_Get_PaymentInfo()
    {
        var moqPaymentServiceClient = new Mock<IPaymentServiceClient>();

        var sut = new GetPaymentQueryService(moqPaymentServiceClient.Object);

        var paymentId = Guid.NewGuid();

        var query = new GetPaymentQuery(paymentId);

        _ = await sut.GetAsync(query);

        moqPaymentServiceClient.Verify(
            fake => fake.GetPaymentByIdAsync(
                It.Is<Guid>(x => x == paymentId),
                It.IsAny<CancellationToken>()),
            Times.Exactly(1));
    }

    [Test]
    public async Task User_Can_List_Available_BrandPayments()
    {
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

        var sut = new BrandPaymentsQueryService(fakePaymentServiceClient, fakePlayerServiceClient);

        var brandId = Guid.NewGuid();
        var playerCode = "ThisIsPlayerCode";

        var query = new BrandPaymentsQuery(brandId, playerCode);

        fakePaymentServiceClient
            .GetBrandPaymentAclsAsync(Arg.Any<string>(), Arg.Any<IEnumerable<Guid>>())
            .Returns(Array.Empty<BrandPaymentAcl>().ToAsyncEnumerable());

        var playerInfo = new Tgs.PlayerService.Client.PlayerInfo
        {
            BrandId = brandId,
            PlayerCode = playerCode,
            IdentityAuthentication = false,
            PhoneAuthentication = false,
            VipLevelId = null
        };

        fakePlayerServiceClient
            .GetPlayerInfoAsync(query.PlayerCode)
            .Returns(playerInfo);

        _ = await sut.QueryAsync(query).ToArrayAsync();

        fakePaymentServiceClient
            .Received(1)
            .GetBrandPaymentAclsAsync(Arg.Any<string>(), Arg.Any<IEnumerable<Guid>>());

        fakePaymentServiceClient
            .Received(1)
            .FilterBrandPaymentsAsync(
                Arg.Any<IEnumerable<BrandPaymentInfo>>(),
                Arg.Any<Tgs.Payment.Clients.PlayerInfo>(),
                Arg.Any<IEnumerable<Guid>>(),
                Arg.Any<IEnumerable<Guid>>(),
                Arg.Any<ILookup<Guid, Guid>>());
    }
}
