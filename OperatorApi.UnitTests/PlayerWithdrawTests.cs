using NSubstitute;
using Tgs.Payment.Clients;
using TGS.Services.Operator.ApplicationServices.Withdraw;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class PlayerWithdrawTests
{
	[Test]
	public async Task Get_Player_Withdraw_Info_Should_Success()
	{
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

		var sut = new WithdrawFormsQueryService(fakePaymentServiceClient);

		var query = new WithdrawFormsQuery(
			BrandId: Guid.NewGuid(),
			"playerCode",
			new List<PaymentFlowState>(),
			CreateOnRange: null,
			UpdateOnRange: null,
			new Paging());

		_ = await sut.GetAsync(query, default);

		_ = fakePaymentServiceClient
			.Received(1)
			.QueryWithdrawFormsAsync(
				Arg.Any<WithdrawFormQueryConditions>(),
				Arg.Any<Tgs.Payment.Clients.Paging>());
	}

	[Test]
	public async Task Player_Query_Withdraw_Forms_Should_Use_UserId_And_UserType_Condition()
	{
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

		var sut = new WithdrawFormsQueryService(fakePaymentServiceClient);

		var query = new WithdrawFormsQuery(
			Guid.NewGuid(),
			"PlayerCode",
			Array.Empty<PaymentFlowState>(),
			null,
			null,
			new Paging());

		_ = await sut.GetAsync(query, default);

		_ = fakePaymentServiceClient
			.Received(1)
			.QueryWithdrawFormsAsync(
				Arg.Is<WithdrawFormQueryConditions>(c => c.UserId == query.PlayerCode && c.UserType == UserType.Player),
				Arg.Any<Tgs.Payment.Clients.Paging>());
	}
}
