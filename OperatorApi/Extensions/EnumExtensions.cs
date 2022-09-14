using System.Collections.Generic;
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
    {
        return status switch
        {
            FlowState.WaitingPayment => new[] { PaymentFlowState.WaitPayment },
            FlowState.Review => new[] { PaymentFlowState.WaitApprove },
            FlowState.Approved => new[] { PaymentFlowState.Approved },
            FlowState.WaitingPaid => new[] { PaymentFlowState.Approved },
            FlowState.Rejected => new[] { PaymentFlowState.Reject },
            FlowState.Paid => new[] { PaymentFlowState.Paid },
            _ => new[] {
                PaymentFlowState.WaitPayment,
                PaymentFlowState.WaitApprove,
                PaymentFlowState.Approved,
                PaymentFlowState.Reject,
                PaymentFlowState.Paid
            }
        };
    }

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
