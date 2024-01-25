using System;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using TGS.Services.Operator.MessageQueue;
using TGS.Services.Operator.Notify;

namespace TGS.Tests.OperatorApi.UnitTests;

public class DepositStatusChangedTests
{
	[Test]
	public async Task 當存款單拒絕時會發出通知()
	{
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakeNotifyEventPublisher = Substitute.For<INotifyEventPublisher>();
		var jsonOptionsAccessor = new OptionsWrapper<JsonOptions>(new JsonOptions());

		var sut = new DepositFormStatusChangedHandler(
			fakePaymentServiceClient,
			fakeNotifyEventPublisher,
			jsonOptionsAccessor,
			NullLogger<DepositFormStatusChangedHandler>.Instance);

		var eventData = new FormEventEx
		{
			BrandId = Guid.NewGuid(),
			FormId = Guid.NewGuid(),
			Status = PaymentFlowState.WaitApprove.ToString()
		};
		var playerCode = "test1";

		_ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(eventData.FormId.ToGuid()))
			.Returns(new DepositFormInfo
			{
				Id = eventData.FormId,
				BrandId = eventData.BrandId,
				PlayerCode = playerCode,
				Status = PaymentFlowState.Reject
			});

		await sut.HandleAsync("test", eventData.ToByteArray(), null);

		fakeNotifyEventPublisher.Received(1)
			.PublishEvent(
				Arg.Is<NotifyEvent>(evt => evt.Match("BrandId", eventData.BrandId.ToGuid().ToString())
					&& evt.Match("Feature", "DepositNotify")
					&& evt.Match("UserId", playerCode)));
	}

	[Test]
	public async Task 當存款單通過時會發出通知()
	{
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakeNotifyEventPublisher = Substitute.For<INotifyEventPublisher>();
		var jsonOptionsAccessor = new OptionsWrapper<JsonOptions>(new JsonOptions());

		var sut = new DepositFormStatusChangedHandler(
			fakePaymentServiceClient,
			fakeNotifyEventPublisher,
			jsonOptionsAccessor,
			NullLogger<DepositFormStatusChangedHandler>.Instance);

		var eventData = new FormEventEx
		{
			BrandId = Guid.NewGuid(),
			FormId = Guid.NewGuid(),
			Status = PaymentFlowState.WaitApprove.ToString()
		};
		var playerCode = "test1";

		_ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(eventData.FormId.ToGuid()))
			.Returns(new DepositFormInfo
			{
				Id = eventData.FormId,
				BrandId = eventData.BrandId,
				PlayerCode = playerCode,
				Status = PaymentFlowState.Approved
			});

		await sut.HandleAsync("test", eventData.ToByteArray(), null);

		fakeNotifyEventPublisher.Received(1)
			.PublishEvent(
				Arg.Is<NotifyEvent>(evt => evt.Match("BrandId", eventData.BrandId.ToGuid().ToString())
					&& evt.Match("Feature", "DepositNotify")
					&& evt.Match("UserId", playerCode)));
	}
}
