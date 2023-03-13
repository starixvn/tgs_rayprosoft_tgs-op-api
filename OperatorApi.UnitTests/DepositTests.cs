using System;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using ExpectedObjects;
using Moq;
using NSubstitute;
using NUnit.Framework;
using Tgs.BrandCertificate.Clients;
using Tgs.FileService.Clients;
using Tgs.Payment.Clients;
using Tgs.Payment.LeePay;
using Tgs.Payment.ManualDeposit;
using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;
using TGS.Core.Infrastructures.Json;
using TGS.Payment.ManualDeposit;
using TGS.Services.Operator;
using TGS.Services.Operator.ApplicationServices.BrandPayments;
using TGS.Services.Operator.ApplicationServices.Deposit;

namespace TGS.Tests.OperatorApi.UnitTests;

[TestFixture]
public class DepositTests
{
	[Test]
	public void Bank_Deposit_Form_If_Not_Player_Own_Will_Throw_Exception()
	{
		var moqPaymentServiceClient = new Mock<IPaymentServiceClient>();

		var sut = new GetBankDepositFromQueryService(moqPaymentServiceClient.Object);

		var query = new GetBankDepositFormQuery(
			"ThisIsPlayerCode",
			Guid.NewGuid(),
			Guid.NewGuid());

		_ = moqPaymentServiceClient
			.Setup(fake => fake.GetDepositFormAsync(
				It.Is<Guid>(x => x == query.BrandId),
				It.Is<Guid>(x => x == query.FormId),
				It.IsAny<CancellationToken>()))
			.Returns(ValueTask.FromResult(new DepositFormInfo
			{
				Amount = 1000M,
				BrandId = Guid.NewGuid(),
				BrandPaymentId = Guid.NewGuid(),
				ChannelName = "ChannelName",
				CreatedOn = DateTimeOffset.Now,
				Id = query.FormId,
				PaymentMethodId = Guid.NewGuid(),
				PlayerCode = "OtherPlayerCode",
				UserName = "OtherUserName",
				Status = PaymentFlowState.WaitApprove,
				UpdatedOn = DateTimeOffset.Now
			}));

		_ = Assert.ThrowsAsync<UltraViresException>(async () => await sut.GetAsync(query));
	}

	[Test]
	public async Task Create_Bank_Deposit_Form_Should_Success()
	{
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var moqPaymentServiceClient = new Mock<IPaymentServiceClient>();
		var moqPaymentClient = new Mock<IPaymentClient>();
		var serializer = new ManualDepositSerializer();
		var moqPromotionClient = new Mock<IPromotionClient>();

		var sut = new CreateDepositFormCommandService(
			fakeBrandServiceClient,
			moqPaymentServiceClient.Object,
			moqPaymentClient.Object,
			moqPromotionClient.Object);

		var command = new CreateDepositFormCommand(
			"ThisIsPlayerCode",
			Guid.NewGuid(),
			Guid.NewGuid(),
			1000M,
			"tgs.payment.manual",
			null,
			_ => serializer.Serialize(
				new ManualDepositBankApplyInfo()
				{
					SendBankAccount = "ThisIsBankAccount",
					ReceiveBankAccount = "ThisIsReceiveBankAccount",
					ReceiveAccountName = "ReceiveAccountName",
					BankType = "Bank"
				}));
		var paymentId = Guid.NewGuid();

		_ = moqPaymentClient
			.Setup(fake => fake.DepositAsync(
				It.IsAny<string>(),
				It.Is<decimal>(x => x == command.Amount),
				It.IsAny<string>(),
				It.IsAny<CancellationToken>()))
			.Returns(ValueTask.FromResult(command.CreatePaymentSettings(Guid.NewGuid())));

		_ = moqPaymentServiceClient
			.Setup(fake => fake.GetBrandPaymentByIdAsync(
				It.Is<Guid>(x => x == command.BrandPaymentId),
				It.IsAny<CancellationToken>()))
			.Returns(ValueTask.FromResult(new BrandPaymentInfo
			{
				Id = command.BrandPaymentId,
				PaymentId = paymentId
			}));

		_ = moqPaymentServiceClient
			.Setup(fake => fake.GetPaymentByIdAsync(
				It.Is<Guid>(x => x == paymentId),
				It.IsAny<CancellationToken>()))
			.Returns(ValueTask.FromResult(new PaymentInfo
			{
				Id = paymentId,
				Code = "Test"
			}));

		var expected = new DepositFormCreateInfo
		{
			PlayerCode = command.PlayerCode,
			BrandId = command.BrandId,
			BrandPaymentId = command.BrandPaymentId,
			Amount = command.Amount,
			PromotionId = command.PromotionId,
			ApplyInfo = serializer.Serialize(
				new ManualDepositBankApplyInfo()
				{
					BankType = "Bank",
					SendBankAccount = "ThisIsBankAccount",
					ReceiveBankAccount = "ThisIsReceiveBankAccount",
					ReceiveAccountName = "ReceiveAccountName"
				})
		}.ToExpectedObject();

		_ = await sut.GetAsync(command);

		moqPaymentServiceClient.Verify(
			fake => fake.CreateDepositFormAsync(
				It.IsAny<Guid>(),
				It.Is<DepositFormCreateInfo>(form => expected.Matches(form)),
				It.IsAny<CancellationToken>()),
			Times.Exactly(1));
	}

	[Test]
	public async Task Create_Crypto_Deposit_Form_Should_Success()
	{
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var moqPaymentServiceClient = new Mock<IPaymentServiceClient>();
		var moqPaymentClient = new Mock<IPaymentClient>();
		var serializer = new LeePaySerializer();
		var moqPromotionClient = new Mock<IPromotionClient>();

		var sut = new CreateDepositFormCommandService(
			fakeBrandServiceClient,
			moqPaymentServiceClient.Object,
			moqPaymentClient.Object,
			moqPromotionClient.Object);

		var createFormId = Guid.NewGuid();
		var command = new CreateDepositFormCommand(
			"ThisIsPlayerCode",
			Guid.NewGuid(),
			Guid.NewGuid(),
			1000M,
			"tgs.payment.leepay",
			null,
			_ => serializer.Serialize(
				new LeePayDepositApplyInfo(
					new(
						"systemId",
						"systemKey",
						createFormId,
						"CNY",
						"http://localhost",
						null),
					new(
						"00",
						null,
						"testOrderId",
						"testOrderNo",
						createFormId,
						"walletAddress",
						"http://test.com",
						11M,
						"CNY",
						0.1M))));
		var paymentId = Guid.NewGuid();

		_ = moqPaymentClient
			.Setup(fake => fake.DepositAsync(
				It.IsAny<string>(),
				It.Is<decimal>(x => x == command.Amount),
				It.IsAny<string>(),
				It.IsAny<CancellationToken>()))
			.Returns(ValueTask.FromResult(command.CreatePaymentSettings(Guid.NewGuid())));

		_ = moqPaymentServiceClient
			.Setup(fake => fake.GetBrandPaymentByIdAsync(
				It.Is<Guid>(x => x == command.BrandPaymentId),
				It.IsAny<CancellationToken>()))
			.Returns(ValueTask.FromResult(new BrandPaymentInfo
			{
				Id = command.BrandPaymentId,
				PaymentId = paymentId
			}));

		_ = moqPaymentServiceClient
			.Setup(fake => fake.GetPaymentByIdAsync(
				It.Is<Guid>(x => x == paymentId),
				It.IsAny<CancellationToken>()))
			.Returns(ValueTask.FromResult(new PaymentInfo
			{
				Id = paymentId,
				Code = "Test"
			}));

		var expected = new DepositFormCreateInfo
		{
			PlayerCode = command.PlayerCode,
			Amount = command.Amount,
			ApplyInfo = serializer.Serialize(
				new LeePayDepositApplyInfo(
					new(
						"systemId",
						"systemKey",
						createFormId,
						"CNY",
						"http://localhost",
						null),
					new(
						"00",
						null,
						"testOrderId",
						"testOrderNo",
						createFormId,
						"walletAddress",
						"http://test.com",
						11M,
						"CNY",
						0.1M))),
			BrandId = command.BrandId,
			BrandPaymentId = command.BrandPaymentId,
			PromotionId = command.PromotionId
		}.ToExpectedObject();

		_ = await sut.GetAsync(command);

		moqPaymentServiceClient.Verify(
			fake => fake.CreateDepositFormAsync(
				It.IsAny<Guid>(),
				It.Is<DepositFormCreateInfo>(form => expected.Matches(form)),
				It.IsAny<CancellationToken>()),
			Times.Exactly(1));
	}

	[Test]
	public void Create_DepositForm_Can_Not_Lock_Payment_Should_Fail()
	{
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakePaymentClient = Substitute.For<IPaymentClient>();
		var fakePromotionClient = Substitute.For<IPromotionClient>();

		var sut = new CreateDepositFormCommandService(
			fakeBrandServiceClient,
			fakePaymentServiceClient,
			fakePaymentClient,
			fakePromotionClient);

		var command = new CreateDepositFormCommand(
			"ThisIsPlayerCode",
			Guid.NewGuid(),
			Guid.NewGuid(),
			1000M,
			"tgs.payment.leepay",
			null,
			_ => "{}");
		var paymentId = Guid.NewGuid();

		_ = fakePaymentServiceClient.GetBrandPaymentByIdAsync(
			Arg.Is(command.BrandPaymentId))
			.Returns(new BrandPaymentInfo
			{
				Id = command.BrandPaymentId,
				PaymentId = paymentId
			});

		_ = fakePaymentServiceClient.GetPaymentByIdAsync(
			Arg.Is(paymentId))
			.Returns(new PaymentInfo
			{
				Id = paymentId,
				Code = "Test"
			});

		_ = fakeBrandServiceClient.GetBrandPaymentSettingsAsync(
			Arg.Is(command.BrandId))
			.Returns(new BrandPaymentSettings(true, false, 0M));

		_ = Assert.ThrowsAsync<PreviousDepositStillProcessingException>(async () => await sut.GetAsync(command));
	}

	[Test]
	public void Create_DepositForm_Not_Found_BrandPayment_Should_Fail()
	{
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakePaymentClient = Substitute.For<IPaymentClient>();
		var fakePromotionClient = Substitute.For<IPromotionClient>();

		var sut = new CreateDepositFormCommandService(
			fakeBrandServiceClient,
			fakePaymentServiceClient,
			fakePaymentClient,
			fakePromotionClient);

		var command = new CreateDepositFormCommand(
			"ThisIsPlayerCode",
			Guid.NewGuid(),
			Guid.NewGuid(),
			1000M,
			"tgs.payment.leepay",
			Guid.NewGuid(),
			_ => "{}");

		_ = Assert.ThrowsAsync<Exception>(async () => await sut.GetAsync(command));
	}

	[Test]
	public void Daily_Second_Deposit_With_First_Deposit_Promotion_Should_Fail()
	{
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var moqPaymentServiceClient = new Mock<IPaymentServiceClient>();
		var moqPaymentClient = new Mock<IPaymentClient>();
		var moqPromotionClient = new Mock<IPromotionClient>();

		var sut = new CreateDepositFormCommandService(
			fakeBrandServiceClient,
			moqPaymentServiceClient.Object,
			moqPaymentClient.Object,
			moqPromotionClient.Object);

		var command = new CreateDepositFormCommand(
			"ThisIsPlayerCode",
			Guid.NewGuid(),
			Guid.NewGuid(),
			1000M,
			"tgs.payment.leepay",
			Guid.NewGuid(),
			_ => "{}");
		var paymentId = Guid.NewGuid();

		_ = moqPaymentServiceClient
			.Setup(fake => fake.GetBrandPaymentByIdAsync(
				It.Is<Guid>(x => x == command.BrandPaymentId),
				It.IsAny<CancellationToken>()))
			.Returns(ValueTask.FromResult(new BrandPaymentInfo
			{
				Id = command.BrandPaymentId,
				PaymentId = paymentId
			}));

		_ = moqPaymentServiceClient
			.Setup(fake => fake.GetPaymentByIdAsync(
				It.Is<Guid>(x => x == paymentId),
				It.IsAny<CancellationToken>()))
			.Returns(ValueTask.FromResult(new PaymentInfo
			{
				Id = paymentId,
				Code = "Test"
			}));

		_ = Assert.ThrowsAsync<CannotJoinPromotionException>(async () => await sut.GetAsync(command));
	}

	[Test]
	public async Task Get_Bank_Deposit_Form_By_Id_Should_Success()
	{
		var moqPaymentServiceClient = new Mock<IPaymentServiceClient>();
		var jsonSerializationProvider = new JsonSerializationProvider();

		var sut = new GetBankDepositFromQueryService(moqPaymentServiceClient.Object);

		var query = new GetBankDepositFormQuery(
			"ThisIsPlayerCode",
			Guid.NewGuid(),
			Guid.NewGuid());

		_ = moqPaymentServiceClient
			.Setup(fake => fake.GetDepositFormAsync(
				It.Is<Guid>(x => x == query.BrandId),
				It.Is<Guid>(x => x == query.FormId),
				It.IsAny<CancellationToken>()))
			.Returns(ValueTask.FromResult(new DepositFormInfo
			{
				Amount = 1000M,
				BrandId = query.BrandId,
				BrandPaymentId = Guid.NewGuid(),
				ChannelName = "ChannelName",
				CreatedOn = DateTimeOffset.Now,
				Id = query.FormId,
				PaymentMethodId = Guid.NewGuid(),
				PlayerCode = query.PlayerCode,
				Status = PaymentFlowState.WaitApprove,
				UpdatedOn = DateTimeOffset.Now,
				UserName = "PlayerName"
			}));

		var actual = await sut.GetAsync(query);

		moqPaymentServiceClient.Verify(
			fake => fake.GetDepositFormAsync(
				It.Is<Guid>(x => x == query.BrandId),
				It.Is<Guid>(x => x == query.FormId),
				It.IsAny<CancellationToken>()),
			Times.Exactly(1));
	}

	[Test]
	public async Task Get_Brand_Payment_Should_Success()
	{
		var moqPaymentServiceClient = new Mock<IPaymentServiceClient>();

		var sut = new GetBrandPaymentQueryService(moqPaymentServiceClient.Object);

		var query = new GetBrandPaymentQuery(Guid.NewGuid());

		_ = await sut.GetAsync(query);

		moqPaymentServiceClient.Verify(
			fake => fake.GetBrandPaymentByIdAsync(
				It.Is<Guid>(x => x == query.Id),
				It.IsAny<CancellationToken>()),
			Times.Exactly(1));
	}

	[Test]
	public async Task Submit_BankDepositForm_Should_Success()
	{
		var moqPaymentServiceClient = new Mock<IPaymentServiceClient>();
		var moqFileServiceClient = new Mock<IFileServiceClient>();
		var moqBrandPaymentQueryService = new Mock<IQueryService<BrandPaymentsQuery, BrandPaymentInfo>>();
		var moqGetPaymentService = new Mock<IGetService<GetPaymentQuery, PaymentInfo>>();

		var sut = new SubmitBankDepositFormCommandService(
			moqPaymentServiceClient.Object,
			moqFileServiceClient.Object,
			moqBrandPaymentQueryService.Object,
			moqGetPaymentService.Object);

		var brandId = Guid.NewGuid();
		var playerCode = "ThisIsPlayerCode";
		var formId = Guid.NewGuid();
		var contentType = "image/jpeg";
		using var fileStream = new MemoryStream();

		var command = new SubmitBankDepositFormCommand(
			brandId,
			playerCode,
			formId,
			contentType,
			fileStream,
			id => "{}");

		_ = moqBrandPaymentQueryService
			.Setup(fake => fake.QueryAsync(
				It.IsAny<BrandPaymentsQuery>(),
				It.IsAny<CancellationToken>()))
			.Returns(new[] {
				new BrandPaymentInfo
				{
					TransferType = TransferType.Deposit
				}
			}.ToAsyncEnumerable());

		_ = moqGetPaymentService
			.Setup(fake => fake.GetAsync(
				It.IsAny<GetPaymentQuery>(),
				It.IsAny<CancellationToken>()))
			.Returns(ValueTask.FromResult(
				new PaymentInfo
				{
					Code = "Manual"
				}));

		_ = moqPaymentServiceClient
			.Setup(fake => fake.GetDepositFormAsync(
				It.Is<Guid>(x => x == command.BrandId),
				It.Is<Guid>(x => x == command.FormId),
				It.IsAny<CancellationToken>()))
			.Returns(ValueTask.FromResult(
				new DepositFormInfo
				{
					Id = Guid.NewGuid(),
					Status = PaymentFlowState.WaitPayment
				}));

		_ = moqFileServiceClient
			.Setup(fake => fake.UploadFileAsync(
				It.IsAny<string>(),
				It.Is<MediaTypeHeaderValue>(x => x.ToString() == contentType),
				It.IsAny<Stream>()))
			.Returns(ValueTask.FromResult(new FileView
			{
				Id = "testFileId",
				ContentType = contentType
			}));

		await sut.ExecuteAsync(command);

		moqFileServiceClient.Verify(
			fake => fake.UploadFileAsync(It.IsAny<string>(), It.IsAny<MediaTypeHeaderValue>(), It.IsAny<Stream>()),
			Times.Exactly(1));
		moqPaymentServiceClient.Verify(
			fake => fake.SubmitDepositFormStatusAsync(
				It.IsAny<Guid>(),
				It.Is<PaymentFlowState>(x => x == PaymentFlowState.WaitApprove),
				It.IsAny<string>(),
				It.IsAny<decimal?>(),
				It.IsAny<string>(),
				It.IsAny<int?>(),
				It.IsAny<decimal?>(),
				It.IsAny<CancellationToken>()),
			Times.Exactly(1));
	}

	[Test]
	public async Task Submit_Manual_DepositForm_If_Status_Not_WaitPayment_Will_No_Action()
	{
		var moqPaymentServiceClient = new Mock<IPaymentServiceClient>();
		var moqFileServiceClient = new Mock<IFileServiceClient>();
		var moqBrandPaymentQueryService = new Mock<IQueryService<BrandPaymentsQuery, BrandPaymentInfo>>();
		var moqGetPaymentService = new Mock<IGetService<GetPaymentQuery, PaymentInfo>>();

		var sut = new SubmitBankDepositFormCommandService(
			moqPaymentServiceClient.Object,
			moqFileServiceClient.Object,
			moqBrandPaymentQueryService.Object,
			moqGetPaymentService.Object);

		var brandId = Guid.NewGuid();
		var playerCode = "ThisIsPlayerCode";
		var formId = Guid.NewGuid();
		var contentType = "image/jpeg";
		using var fileStream = new MemoryStream();

		var command = new SubmitBankDepositFormCommand(
			brandId,
			playerCode,
			formId,
			contentType,
			fileStream,
			id => "{}");

		_ = moqBrandPaymentQueryService
			.Setup(fake => fake.QueryAsync(
				It.IsAny<BrandPaymentsQuery>(),
				It.IsAny<CancellationToken>()))
			.Returns(new[] {
				new BrandPaymentInfo
				{
					TransferType = TransferType.Deposit
				}
			}.ToAsyncEnumerable());

		_ = moqGetPaymentService
			.Setup(fake => fake.GetAsync(
				It.IsAny<GetPaymentQuery>(),
				It.IsAny<CancellationToken>()))
			.Returns(ValueTask.FromResult(
				new PaymentInfo
				{
					Code = "Manual"
				}));

		_ = moqPaymentServiceClient
			.Setup(fake => fake.GetDepositFormAsync(
				It.Is<Guid>(x => x == command.BrandId),
				It.Is<Guid>(x => x == command.FormId),
				It.IsAny<CancellationToken>()))
			.Returns(ValueTask.FromResult(
				new DepositFormInfo
				{
					Id = Guid.NewGuid(),
					Status = PaymentFlowState.Approved
				}));

		_ = moqFileServiceClient
			.Setup(fake => fake.UploadFileAsync(
				It.IsAny<string>(),
				It.Is<MediaTypeHeaderValue>(x => x.ToString() == contentType),
				It.IsAny<Stream>()))
			.Returns(ValueTask.FromResult(new FileView
			{
				Id = "testFileId",
				ContentType = contentType
			}));

		await sut.ExecuteAsync(command);

		moqFileServiceClient.Verify(
			fake => fake.UploadFileAsync(It.IsAny<string>(), It.IsAny<MediaTypeHeaderValue>(), It.IsAny<Stream>()),
			Times.Exactly(0));
		moqPaymentServiceClient.Verify(
			fake => fake.SubmitDepositFormStatusAsync(
				It.IsAny<Guid>(),
				It.Is<PaymentFlowState>(x => x == PaymentFlowState.WaitApprove),
				It.IsAny<string>(),
				It.IsAny<decimal?>(),
				It.IsAny<string>(),
				It.IsAny<int?>(),
				It.IsAny<decimal?>(),
				It.IsAny<CancellationToken>()),
			Times.Exactly(0));
	}

	[Test]
	public async Task User_Can_Get_Deposit_Form()
	{
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

		var sut = new GetDepositFormQueryService(fakePaymentServiceClient);

		var query = new GetDepositFormQuery(
			"ThisIsPlayerCode",
			Guid.NewGuid(),
			Guid.NewGuid());

		_ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(query.BrandId), Arg.Is(query.FormId))
			.Returns(new DepositFormInfo
			{
				Amount = 1000M,
				BrandId = query.BrandId,
				BrandPaymentId = Guid.NewGuid(),
				ChannelName = "ChannelName",
				CreatedOn = DateTimeOffset.Now,
				Id = query.FormId,
				PaymentMethodId = Guid.NewGuid(),
				PlayerCode = query.PlayerCode,
				Status = PaymentFlowState.WaitApprove,
				UpdatedOn = DateTimeOffset.Now,
				UserName = "UserName"
			});

		_ = await sut.GetAsync(query);

		_ = fakePaymentServiceClient.Received(1)
			.GetDepositFormAsync(Arg.Is(query.BrandId), Arg.Is(query.FormId));
	}
}
