using System;

namespace TGS.Services.Operator.ApplicationServices.Player;
public record GetCurrentPlayerRegisterFlowStepQuery(Guid BrandId, string? PlayerCode);