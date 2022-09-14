using System;
using System.Threading;
using System.Threading.Tasks;
using Tgs.BrandCertificate.Clients;
using Tgs.Payment.Clients;
using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;

namespace TGS.Services.Operator.ApplicationServices.Deposit;

internal class CreateDepositFormCommandService : IGetService<CreateDepositFormCommand, Guid>
{
    private readonly IBrandServiceClient _brandServiceClient;
    private readonly IPaymentClient _paymentClient;
    private readonly IPaymentServiceClient _paymentServiceClient;
    private readonly IPromotionClient _promotionClient;

    public CreateDepositFormCommandService(
        IBrandServiceClient brandServiceClient,
        IPaymentServiceClient paymentServiceClient,
        IPaymentClient paymentClient,
        IPromotionClient promotionClient)
    {
        _brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
        _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
        _paymentClient = paymentClient ?? throw new ArgumentNullException(nameof(paymentClient));
        _promotionClient = promotionClient ?? throw new ArgumentNullException(nameof(promotionClient));
    }

    public async ValueTask<Guid> GetAsync(CreateDepositFormCommand command, CancellationToken cancellationToken = default)
    {
        var createFormId = Guid.NewGuid();

        var brandPayment = await _paymentServiceClient
            .GetBrandPaymentByIdAsync(command.BrandPaymentId, cancellationToken)
            .ConfigureAwait(false);

        if (brandPayment is null)
            throw new Exception("BrandPayment can't found.");

        var payment = await _paymentServiceClient
            .GetPaymentByIdAsync(brandPayment.PaymentId)
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
            .LockDepositPaymentAsync(createFormId, payment!.Code, command.PlayerCode)
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
                .DepositAsync(command.Subject, command.Amount, settings)
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

            await _paymentServiceClient
                .CreateDepositFormAsync(createFormId, form)
                .ConfigureAwait(false);
        }
        catch
        {
            await Task.WhenAll(
                _paymentServiceClient.UnlockDepositPaymentAsync(
                    createFormId,
                    payment.Code,
                    command.PlayerCode).AsTask(),
                _promotionClient.UntagPlayerDepositAsync(command.PlayerCode, command.PromotionId))
                .ConfigureAwait(false);
            throw;
        }

        await _paymentServiceClient
            .SubmitDepositFormStatusAsync(createFormId, PaymentFlowState.WaitPayment, null)
            .ConfigureAwait(false);

        return createFormId;
    }
}
