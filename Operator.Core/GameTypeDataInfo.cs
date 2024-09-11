using System;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator;

public record GameTypeDataInfo(Guid GameProviderId, Core.Models.Enums.GameType Type, string SubGameProviderCode);