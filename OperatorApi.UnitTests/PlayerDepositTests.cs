using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Tgs.Payment.Clients;
using TGS.Core.Models.Messages;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class PlayerDepositTests
{
    [Test]
    public async Task Get_Player_Deposit_Info_Should_Success()
    {
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

        var sut = new QueryDepositFormsService(fakePaymentServiceClient);

        var query = new DepositFormsQuery(
            BrandId: Guid.NewGuid(),
            "playerCode",
            new List<PaymentFlowState>(),
            CreateOnRange: null,
            UpdateOnRange: null,
            new PageableRequest());

        await sut.GetAsync(query, default);

        _ = fakePaymentServiceClient
            .Received(1)
            .QueryDepositFormsAsync(
                Arg.Any<DepositFormQueryConditions>(),
                Arg.Any<Tgs.Payment.Clients.Paging>());
    }

    [Test]
    public async Task Get_Player_Deposit_Info_Should_Use_UserId_Condition()
    {
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

        var sut = new QueryDepositFormsService(fakePaymentServiceClient);

        var query = new DepositFormsQuery(
            BrandId: Guid.NewGuid(),
            "playerCode",
            new List<PaymentFlowState>(),
            CreateOnRange: null,
            UpdateOnRange: null,
            new PageableRequest());

        await sut.GetAsync(query, default);

        _ = fakePaymentServiceClient
            .Received(1)
            .QueryDepositFormsAsync(
                Arg.Is<DepositFormQueryConditions>(c => c.UserId == query.PlayerCode),
                Arg.Any<Tgs.Payment.Clients.Paging>());
    }
}
