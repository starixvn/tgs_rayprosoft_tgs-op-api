using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Tgs.FileService.Clients;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;
using TGS.Core.Infrastructures.Utility;
using TGS.Services.Operator.ApplicationServices.BrandPayments;

namespace TGS.Services.Operator.ApplicationServices.Deposit;

internal class SubmitBankDepositFormCommandService(
	IPaymentServiceClient paymentServiceClient,
	IFileServiceClient fileServiceClient,
	IQueryService<BrandPaymentsQuery, BrandPaymentInfo> brandPaymentsQueryService,
	IGetService<GetPaymentQuery, PaymentInfo> getPaymentService) : ICommandService<SubmitBankDepositFormCommand>
{
	private readonly IQueryService<BrandPaymentsQuery, BrandPaymentInfo> _brandPaymentsQueryService = brandPaymentsQueryService ?? throw new ArgumentNullException(nameof(brandPaymentsQueryService));
	private readonly IFileServiceClient _fileServiceClient = fileServiceClient ?? throw new ArgumentNullException(nameof(fileServiceClient));
	private readonly IGetService<GetPaymentQuery, PaymentInfo> _getPaymentService = getPaymentService ?? throw new ArgumentNullException(nameof(getPaymentService));
	private readonly IPaymentServiceClient _paymentServiceClient =
			paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));

	public async ValueTask ExecuteAsync(SubmitBankDepositFormCommand command,
		CancellationToken cancellationToken = default)
	{
		var formData = await _paymentServiceClient
			.GetDepositFormAsync(
				command.BrandId,
				command.FormId, cancellationToken)
			.ConfigureAwait(false);

		if (formData?.Status != PaymentFlowState.WaitPayment)
			return;

		var brandPayment = await _brandPaymentsQueryService
			.QueryAsync(new BrandPaymentsQuery(command.BrandId, command.PlayerCode), cancellationToken)
			.Where(bp => bp.Id == formData.BrandPaymentId)
			.FirstOrDefaultAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		if (brandPayment?.TransferType != TransferType.Deposit)
			return;

		var paymentInfo = await _getPaymentService
			.GetAsync(new GetPaymentQuery(brandPayment.PaymentId), cancellationToken)
			.ConfigureAwait(false);

		if (paymentInfo?.Code != "Manual")
			return;

		var randomFileName = StringGenerator.GenerateAlphanumericString(32);

		var uploadResult = await _fileServiceClient
			.UploadFileAsync(
				$"/{randomFileName}",
				MediaTypeHeaderValue.Parse(command.ContentType),
				command.FileStream)
			.ConfigureAwait(false);

		await _paymentServiceClient
			.SubmitDepositFormStatusAsync(
				command.FormId,
				PaymentFlowState.WaitApprove,
				command.CreatePaymentReturn(uploadResult.Id),
				cancellationToken: cancellationToken)
			.ConfigureAwait(false);
	}
}
