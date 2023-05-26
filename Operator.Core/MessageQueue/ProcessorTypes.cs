using System;

namespace TGS.Services.Operator.MessageQueue;

public static class ProcessorTypes
{
	public static readonly Type BrandFrontendInfoChangedEventHandlerType = typeof(BrandFrontendInfoChangedEventHandler);
	public static readonly Type DepositFormStatusChangedHandlerType = typeof(DepositFormStatusChangedHandler);
	public static readonly Type NotificationHandlerType = typeof(NotificationHandler);
	public static readonly Type WithdrawFormStatusChangedEventHandlerType = typeof(WithdrawFormStatusChangedEventHandler);
}
