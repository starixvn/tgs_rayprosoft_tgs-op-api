using System;

namespace TGS.Services.Operator.ApplicationServices.Player;

public record ValidatePlayerLoginRecordCommand(Guid LoginRecordId, string PlayerCode);