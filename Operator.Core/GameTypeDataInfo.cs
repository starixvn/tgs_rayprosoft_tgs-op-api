using System;
using TGS.Core.Models.Enums;

namespace TGS.Services.Operator;

public record GameTypeDataInfo(Guid GameProviderId, GameType Type, string SubGameProviderCode);
