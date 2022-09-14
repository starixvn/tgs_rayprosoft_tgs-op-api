using System;

namespace TGS.Services.Operator.ApplicationServices.Deposit;

public record SubmitDepositFormThirdPartyOrderNoCommand(Guid FormId, string? ThirdPartyOrderNo);
