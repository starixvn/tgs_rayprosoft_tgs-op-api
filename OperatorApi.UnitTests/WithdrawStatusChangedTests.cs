using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSubstitute;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using TGS.Services.Operator.MessageQueue;
using TGS.Services.Operator.Notify;

namespace TGS.Tests.OperatorApi.UnitTests;

public class WithdrawStatusChangedTests
{
	[Test]
	public async Task 只有玩家的提款單才會發出通知()
	{
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakeNotifyEventPublisher = Substitute.For<INotifyEventPublisher>();
		var jsonOptionsAccessor = new OptionsWrapper<JsonOptions>(new JsonOptions());

		var sut = new WithdrawFormStatusChangedEventHandler(
			fakePaymentServiceClient,
			fakeNotifyEventPublisher,
			jsonOptionsAccessor);

		var eventData = new FormEventEx
		{
			BrandId = Guid.NewGuid(),
			FormId = Guid.NewGuid(),
			Status = PaymentFlowState.WaitApprove.ToString()
		};
		var agentId = Guid.NewGuid();

		_ = fakePaymentServiceClient.GetWithdrawFormAsync(Arg.Is(eventData.FormId.ToGuid()))
			.Returns(new WithdrawFormInfo
			{
				Id = eventData.FormId,
				BrandId = eventData.BrandId,
				UserType = UserType.Agent,
				UserId = agentId.ToString(),
				Status = PaymentFlowState.Approved
			});

		await sut.HandleAsync("test", eventData.ToByteArray(), null);

		fakeNotifyEventPublisher.DidNotReceive()
			.PublishEvent(
				Arg.Any<NotifyEvent>());
	}

	[Test]
	public async Task 當提款單已支付的時候會發出通知()
	{
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakeNotifyEventPublisher = Substitute.For<INotifyEventPublisher>();
		var jsonOptionsAccessor = new OptionsWrapper<JsonOptions>(new JsonOptions());

		var sut = new WithdrawFormStatusChangedEventHandler(
			fakePaymentServiceClient,
			fakeNotifyEventPublisher,
			jsonOptionsAccessor);

		var eventData = new FormEventEx
		{
			BrandId = Guid.NewGuid(),
			FormId = Guid.NewGuid(),
			Status = PaymentFlowState.Approved.ToString()
		};
		var playerCode = "test1";

		_ = fakePaymentServiceClient.GetWithdrawFormAsync(Arg.Is(eventData.FormId.ToGuid()))
			.Returns(new WithdrawFormInfo
			{
				Id = eventData.FormId,
				BrandId = eventData.BrandId,
				UserType = UserType.Player,
				UserId = playerCode,
				Status = PaymentFlowState.Paid
			});

		await sut.HandleAsync("test", eventData.ToByteArray(), null);

		fakeNotifyEventPublisher.Received(1)
			.PublishEvent(
				Arg.Is<NotifyEvent>(evt => evt.Match("BrandId", eventData.BrandId.ToGuid().ToString())
					&& evt.Match("Feature", "WithdrawNotify")
					&& evt.Match("UserId", playerCode)));
	}

	[Test]
	public async Task 當提款單拒絕的時候會發出通知()
	{
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakeNotifyEventPublisher = Substitute.For<INotifyEventPublisher>();
		var jsonOptionsAccessor = new OptionsWrapper<JsonOptions>(new JsonOptions());

		var sut = new WithdrawFormStatusChangedEventHandler(
			fakePaymentServiceClient,
			fakeNotifyEventPublisher,
			jsonOptionsAccessor);

		var eventData = new FormEventEx
		{
			BrandId = Guid.NewGuid(),
			FormId = Guid.NewGuid(),
			Status = PaymentFlowState.WaitApprove.ToString()
		};
		var playerCode = "test1";

		_ = fakePaymentServiceClient.GetWithdrawFormAsync(Arg.Is(eventData.FormId.ToGuid()))
			.Returns(new WithdrawFormInfo
			{
				Id = eventData.FormId,
				BrandId = eventData.BrandId,
				UserType = UserType.Player,
				UserId = playerCode,
				Status = PaymentFlowState.Reject
			});

		await sut.HandleAsync("test", eventData.ToByteArray(), null);

		fakeNotifyEventPublisher.Received(1)
			.PublishEvent(
				Arg.Is<NotifyEvent>(evt => evt.Match("BrandId", eventData.BrandId.ToGuid().ToString())
					&& evt.Match("Feature", "WithdrawNotify")
					&& evt.Match("UserId", playerCode)));
	}

	[Test]
	public async Task 當提款單審核通過時會發出通知()
	{
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakeNotifyEventPublisher = Substitute.For<INotifyEventPublisher>();
		var jsonOptionsAccessor = new OptionsWrapper<JsonOptions>(new JsonOptions());

		var sut = new WithdrawFormStatusChangedEventHandler(
			fakePaymentServiceClient,
			fakeNotifyEventPublisher,
			jsonOptionsAccessor);

		var eventData = new FormEventEx
		{
			BrandId = Guid.NewGuid(),
			FormId = Guid.NewGuid(),
			Status = PaymentFlowState.WaitApprove.ToString()
		};
		var playerCode = "test1";

		_ = fakePaymentServiceClient.GetWithdrawFormAsync(Arg.Is(eventData.FormId.ToGuid()))
			.Returns(new WithdrawFormInfo
			{
				Id = eventData.FormId,
				BrandId = eventData.BrandId,
				UserType = UserType.Player,
				UserId = playerCode,
				Status = PaymentFlowState.Approved
			});

		await sut.HandleAsync("test", eventData.ToByteArray(), null);

		fakeNotifyEventPublisher.Received(1)
			.PublishEvent(
				Arg.Is<NotifyEvent>(evt => evt.Match("BrandId", eventData.BrandId.ToGuid().ToString())
					&& evt.Match("Feature", "WithdrawNotify")
					&& evt.Match("UserId", playerCode)));
	}
}
