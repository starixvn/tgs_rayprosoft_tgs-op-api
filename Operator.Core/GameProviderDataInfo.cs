using System;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator;

public record GameProviderDataInfo(Guid Id, string Code, Status Status, bool IsTransferWallet);
