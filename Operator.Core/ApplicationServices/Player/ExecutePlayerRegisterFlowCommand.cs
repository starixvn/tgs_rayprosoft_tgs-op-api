using System;
using System.Text.Json;

namespace TGS.Services.Operator.ApplicationServices.Player;
public record ExecutePlayerRegisterFlowCommand(
    Guid BrandId,
    JsonDocument Input,
    PlayerLocationInfo LocationInfo);
