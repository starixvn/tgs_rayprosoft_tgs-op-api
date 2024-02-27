using Tgs.Payment.Clients;
using TGS.Apis.OperatorApi.Models.Messages;

namespace TGS.Apis.OperatorApi.Extensions;

public static class EnumExtensions
{
	public static FlowState ToDepositFlowState(this PaymentFlowState status)
		=> status switch
		{
			PaymentFlowState.WaitPayment => FlowState.WaitingPayment,
			PaymentFlowState.WaitApprove => FlowState.Review,
			PaymentFlowState.Reject => FlowState.Rejected,
			PaymentFlowState.Approved => FlowState.Approved,
			_ => FlowState.Unknown,
		};

	public static IEnumerable<PaymentFlowState> ToPaymentFlowState(this FlowState? status)
		=> status switch
		{
			FlowState.WaitingPayment => [PaymentFlowState.WaitPayment],
			FlowState.Review => [PaymentFlowState.WaitApprove],
			FlowState.Approved => [PaymentFlowState.Approved],
			FlowState.WaitingPaid => [PaymentFlowState.Approved],
			FlowState.Rejected => [PaymentFlowState.Reject],
			FlowState.Paid => [PaymentFlowState.Paid],
			_ => new[] {
				PaymentFlowState.WaitPayment,
				PaymentFlowState.WaitApprove,
				PaymentFlowState.Approved,
				PaymentFlowState.Reject,
				PaymentFlowState.Paid
			}
		};

	public static FlowState ToWithdrawFlowState(this PaymentFlowState status)
		=> status switch
		{
			PaymentFlowState.WaitPayment => FlowState.WaitingPayment,
			PaymentFlowState.WaitApprove => FlowState.Review,
			PaymentFlowState.Reject => FlowState.Rejected,
			PaymentFlowState.Approved => FlowState.WaitingPaid,
			PaymentFlowState.Paid => FlowState.Paid,
			_ => FlowState.Unknown,
		};
}