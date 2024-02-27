namespace TGS.Apis.OperatorApi.Models.Messages;

public enum FlowState
{
	Unknown = 0,
	WaitingPayment,
	Review,
	Approved,
	Rejected,
	WaitingPaid,
	Paid
}