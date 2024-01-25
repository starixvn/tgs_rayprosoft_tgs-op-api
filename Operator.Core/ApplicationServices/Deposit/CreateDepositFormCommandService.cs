using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.BrandCertificate.Clients;
using Tgs.Payment.Clients;
using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Deposit;

internal class CreateDepositFormCommandService(
	IBrandServiceClient brandServiceClient,
	IPaymentServiceClient paymentServiceClient,
	IPaymentClient paymentClient,
	IPromotionClient promotionClient) : IGetService<CreateDepositFormCommand, Guid>
{
	private readonly IBrandServiceClient _brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
	private readonly IPaymentClient _paymentClient = paymentClient ?? throw new ArgumentNullException(nameof(paymentClient));
	private readonly IPaymentServiceClient _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
	private readonly IPromotionClient _promotionClient = promotionClient ?? throw new ArgumentNullException(nameof(promotionClient));

	public async ValueTask<Guid> GetAsync(CreateDepositFormCommand command, CancellationToken cancellationToken = default)
	{
		var createFormId = Guid.NewGuid();

		var brandPayment = await _paymentServiceClient
			.GetBrandPaymentByIdAsync(command.BrandPaymentId, cancellationToken)
			.ConfigureAwait(false)
			?? throw new Exception("BrandPayment can't found.");

		var payment = await _paymentServiceClient
			.GetPaymentByIdAsync(brandPayment.PaymentId, cancellationToken)
			.ConfigureAwait(false);

		if (command.PromotionId != null)
		{
			var canJoin = await _promotionClient.CheckCanJoinDepositPromotionAsync(
				command.BrandId,
				command.PlayerCode,
				command.PromotionId.Value,
				command.Amount).ConfigureAwait(false);

			if (!canJoin)
				throw new CannotJoinPromotionException();
		}

		var brandPaymentSettings = await _brandServiceClient
			.GetBrandPaymentSettingsAsync(command.BrandId, cancellationToken)
			.ConfigureAwait(false);

		var mutexId = await _paymentServiceClient
			.LockDepositPaymentAsync(createFormId, payment!.Code, command.PlayerCode, cancellationToken)
			.ConfigureAwait(false);

		if (brandPaymentSettings is not null && brandPaymentSettings.PaymentDepositLockEnabled && mutexId != createFormId)
			throw new PreviousDepositStillProcessingException();

		try
		{
			await _promotionClient
				.TagPlayerDepositAsync(command.PlayerCode, command.PromotionId)
				.ConfigureAwait(false);

			var settings = command.CreatePaymentSettings(createFormId);
			var applyInfo = await _paymentClient
				.DepositAsync(command.Subject, command.Amount, settings, cancellationToken)
				.ConfigureAwait(false);

			var form = new DepositFormCreateInfo
			{
				Amount = command.Amount,
				ApplyInfo = applyInfo,
				BrandId = command.BrandId,
				BrandPaymentId = command.BrandPaymentId,
				PlayerCode = command.PlayerCode,
				PromotionId = command.PromotionId
			};

			_ = await _paymentServiceClient
				.CreateDepositFormAsync(createFormId, form, cancellationToken)
				.ConfigureAwait(false);
		}
		catch
		{
			await Task.WhenAll(
				_paymentServiceClient.UnlockDepositPaymentAsync(
					createFormId,
					payment.Code,
					command.PlayerCode,
					cancellationToken: cancellationToken).AsTask(),
				_promotionClient.UntagPlayerDepositAsync(command.PlayerCode, command.PromotionId))
				.ConfigureAwait(false);
			throw;
		}

		await _paymentServiceClient
			.SubmitDepositFormStatusAsync(createFormId, PaymentFlowState.WaitPayment, null, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return createFormId;
	}
}
